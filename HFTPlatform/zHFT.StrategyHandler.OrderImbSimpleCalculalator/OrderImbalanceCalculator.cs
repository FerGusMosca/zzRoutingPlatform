using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.OrderImbSimpleCalculator.BusinessEntities;
using zHFT.OrderImbSimpleCalculator.Common.Configuration;
using zHFT.OrderImbSimpleCalculator.Common.Enums;
using zHFT.OrderImbSimpleCalculator.Common.Util;
using zHFT.OrderImbSimpleCalculator.DataAccessLayer;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.DTO;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;
using MarketTimer = zHFT.OrderImbSimpleCalculator.Common.Util.MarketTimer;


namespace zHFT.StrategyHandler.OrderImbSimpleCalculator
{
    public abstract class OrderImbalanceCalculator : ICommunicationModule,ILogger
    {
        #region Protected Attributes

        protected int NextPosId { get; set; }

        protected ICommunicationModule OrderRouter { get; set; }

        protected Dictionary<string, BasePortfImbalancePosition> PortfolioPositions { get; set; }

        protected Dictionary<string, BasePortfImbalancePosition> PendingCancels { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        public string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected IConfiguration Config { get; set; }

        protected zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration Configuration
        {
            get { return (zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected Dictionary<string, BaseMonSecurityImbalance> SecurityImbalancesToMonitor { get; set; }

        protected object tLock { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected SecurityImbalanceManager SecurityImbalanceManager { get; set; }

        protected ImbalancePositionManager ImbalancePositionManager { get; set; }

        protected DateTime? LastPersistanceTime {get;set;}

        protected DateTime StartTime { get; set; }

        protected DateTime LastCounterResetTime { get; set; }

        protected Thread SecImbalancePersistanceThread { get; set; }

        protected Thread ResetEveryNMinutesThread { get; set; }
        
        protected Thread CloseOnTradingTimeOffThread { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }

        protected List<Security> Securities { get; set; }

        protected CustomImbalanceConfigManager CustomImbalanceConfigManager { get; set; }

        #endregion

        #region Load Methods

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
            {
                if(Configuration!=null)
                    OnLogMsg(string.Format("{0}:{1}", Configuration.Name, msg), type);
                else
                    OnLogMsg(string.Format("{0}:{1}", "OrderImbalanceCalculator", msg), type);
            }
        }

        public virtual  void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration().GetConfiguration<zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration>(configFile, noValueFields);
        }

       
        #endregion

        #region Private Methods

        private void LoadPreviousImbalancePositions()
        {
            List<BasePortfImbalancePosition> imbalancePositions = ImbalancePositionManager.GetImbalancePositions(Configuration.Name, true);

            imbalancePositions.ForEach(x => PortfolioPositions.Add(x.OpeningPosition.Security.Symbol, x));
        
        }

        private bool MustPersistFlag()
        {
            if(Configuration.SaveEvery == SaveEvery.HOUR.ToString())
            {
                return  DateTime.Now.Minute==0 && 
                        DateTime.Now.Second<10 && //10 segundos de gracia
                        (!LastPersistanceTime.HasValue ||  (DateTime.Now- LastPersistanceTime.Value).TotalMinutes>50);
            }
            else if (Configuration.SaveEvery == SaveEvery._10MIN.ToString())
            {
                return (DateTime.Now.Minute % 10) == 0 &&
                        DateTime.Now.Second < 10 && //10 segundos de gracia
                        (!LastPersistanceTime.HasValue || (DateTime.Now - LastPersistanceTime.Value).TotalMinutes > 9);
            }
            else if (Configuration.SaveEvery == SaveEvery._30MIN.ToString())
            {
                return (DateTime.Now.Minute == 0 || DateTime.Now.Minute == 30) &&
                        DateTime.Now.Second < 10 && //10 segundos de gracia
                        (!LastPersistanceTime.HasValue || (DateTime.Now - LastPersistanceTime.Value).TotalMinutes > 29);
            }
            else
                throw new Exception(string.Format("SaveEvery {0} not implemented",Configuration.SaveEvery));
        }

        private DateTime GetPersistanceTime()
        {
            if (Configuration.SaveEvery == SaveEvery.HOUR.ToString())
            {
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            }
            if (Configuration.SaveEvery == SaveEvery._30MIN.ToString())
            {
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
            }
            if (Configuration.SaveEvery == SaveEvery._10MIN.ToString())
            {
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
            }
            else
                throw new Exception(string.Format("GetPersistanceTime {0} not implemented", Configuration.SaveEvery));
        }

        private void CloseOnTradingTimeOff()
        {
            try
            {
                while (true)
                {
                    bool foundToClose = false;
                    lock (tLock)
                    {
                        if (!IsTradingTime())
                        {
                            foreach (BaseMonSecurityImbalance secImb in SecurityImbalancesToMonitor.Values)
                            {
                                if (PortfolioPositions.ContainsKey(secImb.Security.Symbol))
                                {
                                    BasePortfImbalancePosition imbPos = PortfolioPositions[secImb.Security.Symbol];
                                    if (imbPos.OpeningPosition != null)
                                    {
                                        foundToClose = true;
                                        RunClose(imbPos.OpeningPosition, secImb, imbPos);
                                        DoLog(
                                            string.Format(
                                                "{0} Position Closed on trading closed @{3}. Symbol {1} Qty={2} ",
                                                imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol,
                                                imbPos.Qty,Configuration.ClosingTime), Constants.MessageType.Information);
                                    }

                                }
                            }
                        }
                    }

                    if (!foundToClose)
                        Thread.Sleep(10 * 1000); //10 seconds
                    else
                        Thread.Sleep(10 * 60 * 1000); //10 minutes

                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("Critical ERROR on CloseOnTradingTimeOff:{0}", e.Message),Constants.MessageType.Error);
            }
            
        }

        private void ResetEveryNMinutes(object param)
        {
            if (Configuration.BlockSizeInMinutes == 0 || Configuration.ActiveBlocks == 0)
                return;

            while (true)
            {
                TimeSpan elapsed = DateTime.Now - LastCounterResetTime;

                if (!zHFT.OrderImbSimpleCalculator.Common.Util.MarketTimer.ValidMarketTime(Configuration.MarketStartTime, Configuration.MarketEndTime))
                {
                    foreach (BaseMonSecurityImbalance secImb in SecurityImbalancesToMonitor.Values)
                    {
                        secImb.ImbalanceCounter.ResetCounters();
                        
                    }
                    LastCounterResetTime = DateTime.Now;
                    StartTime = DateTime.Now;
                
                }

                //Every BlockSize y save what I had in memory with the counters for every position
                if (elapsed.TotalMinutes > Configuration.BlockSizeInMinutes)
                {
                    lock (tLock)
                    {
                        foreach (BaseMonSecurityImbalance secImb in SecurityImbalancesToMonitor.Values)
                        {
                            secImb.ImbalanceCounter.PersistCounters();

                            //We subscratct the values trades that happened longer than (AcitveBlocks+1)*BlockSizeInMinutes
                            if (secImb.ImbalanceCounter.ActiveBlocks.Count > Configuration.ActiveBlocks)
                                secImb.ImbalanceCounter.ResetOldBlocks();
                        }

                        LastCounterResetTime = DateTime.Now;
                    }
                }

                Thread.Sleep(1000);
            }
        }

        private void ImbalancePersistanceThread(object param)
        {
            while (true)
            {


                lock (tLock)
                {
                    try
                    {
                        if (MustPersistFlag())
                        {
                            foreach (BaseMonSecurityImbalance secImb in SecurityImbalancesToMonitor.Values)
                            {
                                secImb.DateTime = GetPersistanceTime();
                                SecurityImbalanceManager.PersistSecurityImbalance(secImb);
                                if (Configuration.ResetOnPersistance)
                                {
                                    secImb.ResetAll();
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog("Error processing ImbalancePersistanceThread @" + Configuration.Name + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            
                Thread.Sleep(5000);
            }
        }

        private void RequestOrderStatuses()
        {
            OrderMassStatusRequestWrapper omsReq = new OrderMassStatusRequestWrapper();
            
            OrderRouter.ProcessMessage(omsReq);
        }

        protected abstract void LoadMonitorsAndRequestMarketData();

        //protected  virtual void LoadMonitorsAndRequestMarketData()
        //{
        //    Thread.Sleep(5000);
        //    foreach (string symbol in Configuration.StocksToMonitor)
        //    {
        //        if (!SecurityImbalancesToMonitor.ContainsKey(symbol))
        //        {


        //            Security sec = new Security()
        //            {
        //                Symbol = symbol,
        //                SecType = Security.GetSecurityType(Configuration.SecurityTypes),
        //                MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
        //                Currency = Configuration.Currency,
        //                Exchange = Configuration.Exchange
        //            };

        //            BaseMonSecurityImbalance secImbalance = new BaseMonSecurityImbalance()
        //            {
        //                Security = sec,
        //                DecimalRounding = Configuration.DecimalRounding,
        //            };



        //            //1- We add the current security to monitor
        //            SecurityImbalancesToMonitor.Add(symbol, secImbalance);

        //            Securities.Add(sec);//So far, this is all wehave regarding the Securities

        //            //2- We request market data

        //            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
        //            MarketDataRequestCounter++;
        //            OnMessageRcv(wrapper);
        //        }
        //    }
        //}

        protected virtual void InitializeCommunicationModules(OnLogMessage pOnLogMsg)
        {
            DoLog("Initializing Order Router " + Configuration.OrderRouter, Constants.MessageType.Information);
            if (!string.IsNullOrEmpty(Configuration.OrderRouter))
            {
                var typeOrderRouter = Type.GetType(Configuration.OrderRouter);
                if (typeOrderRouter != null)
                {
                    OrderRouter = (ICommunicationModule)Activator.CreateInstance(typeOrderRouter);
                    OrderRouter.Initialize(ProcessOutgoing, pOnLogMsg, Configuration.OrderRouterConfigFile);
                }
                else
                    throw new Exception("assembly not found: " + Configuration.OrderRouter);
            }
            else
                DoLog("Order Router not found. It will not be initialized", Constants.MessageType.Error);

        }

        protected virtual BasePortfImbalancePosition LoadNewRegularPos(BaseMonSecurityImbalance secImb, Side side)
        {

            Position routingPos = new Position()
            {
//                Security = new Security()
//                {
//                    Symbol = secImb.Security.Symbol,
//                    MarketData = null,
//                    Currency = Configuration.Currency,
//                    SecType = Security.GetSecurityType(Configuration.SecurityTypes)
//                },
                Security = secImb.Security,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Configuration.PositionSizeInCash,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                AccountId = "",
            };

            routingPos.LoadPosId(NextPosId);
            NextPosId++;

            return new BasePortfImbalancePosition()
            {
                StrategyName = Configuration.Name,
                OpeningDate = DateTime.Now,
                OpeningPosition = routingPos,
                OpeningImbalance = secImb,
                FeeTypePerTrade = Configuration.FeeTypePerTrade,
                FeeValuePerTrade = Configuration.FeeValuePerTrade
            };
        
        }

        private FutureImbalancePosition LoadNewFuturePos(BaseMonSecurityImbalance secImb, Side side)
        {
            Position pos = new Position()
            {
//                Security = new Security()
//                {
//                    Symbol = secImb.Security.Symbol,
//                    MarketData = null,
//                    Currency = Configuration.Currency,
//                    SecType = Security.GetSecurityType(Configuration.SecurityTypes)
//                },
                Security = secImb.Security,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Configuration.PositionSizeInCash.HasValue ? (double?)Configuration.PositionSizeInCash.Value : null,
                Qty = Configuration.PositionSizeInContracts.HasValue ? (double?)Configuration.PositionSizeInContracts.Value : null,
                QuantityType = Configuration.PositionSizeInContracts.HasValue ? QuantityType.CONTRACTS : QuantityType.OTHER,//Tenemos un monto en dólars, pero es el ruteador de ordenes el que decide a cuantos contratos equivale
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                AccountId = "",
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            return new FutureImbalancePosition()
            {
                StrategyName = Configuration.Name,
                Margin = secImb.Security.MarginRatio.HasValue ? secImb.Security.MarginRatio.Value : 1,
                ContractSize = secImb.Security.ContractSize.HasValue ? (double)secImb.Security.ContractSize.Value : 1,
                OpeningDate = DateTime.Now,
                OpeningPosition = pos,
                OpeningImbalance = secImb,
                FeeTypePerTrade = Configuration.FeeTypePerTrade,
                FeeValuePerTrade = Configuration.FeeValuePerTrade
            };
        
        }

        private BasePortfImbalancePosition LoadNewPos(BaseMonSecurityImbalance secImb, Side side)
        {

            if (Security.GetSecurityType(Configuration.SecurityTypes) == SecurityType.FUT)
                return LoadNewFuturePos(secImb, side);
            else
                return LoadNewRegularPos(secImb, side);
        }

        private void LoadCloseFuturePos(Position openPos, BaseMonSecurityImbalance secImb, BasePortfImbalancePosition imbPos)
        {
            Position pos = new Position()
            {
//                Security = new Security()
//                {
//                    Symbol = secImb.Security.Symbol,
//                    MarketData = null,
//                    Currency = Configuration.Currency,
//                    SecType = Security.GetSecurityType(Configuration.SecurityTypes)
//                },
                Security = secImb.Security,
                Side = openPos.Side == Side.Buy ? Side.Sell : Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.CONTRACTS ,//Tenemos un monto en dólars, pero es el ruteador de ordenes el que decide a cuantos contratos equivale
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                AccountId = "",
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            imbPos.ClosingPosition = pos;
            imbPos.ClosingDate = DateTime.Now;
            imbPos.ClosingImbalance = secImb;
        
        }

        private void LoadCloseRegularPos(Position openPos, BaseMonSecurityImbalance secImb, BasePortfImbalancePosition imbPos)
        {
            Position pos = new Position()
            {
//                Security = new Security()
//                {
//                    Symbol = openPos.Security.Symbol,
//                    MarketData = null,
//                    Currency = Configuration.Currency,
//                    SecType = SecurityType.CS
//                },
                Security = secImb.Security,
                Side = openPos.Side == Side.Buy ? Side.Sell : Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.SHARES,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = ""
            };


            pos.PositionCleared = true;
            pos.LoadPosId(NextPosId);
            NextPosId++;

            imbPos.ClosingPosition = pos;
            imbPos.ClosingDate = DateTime.Now;
            imbPos.ClosingImbalance = secImb;
        
        }

        private void PendingCancelTimeoutThread(object symbol)
        {
            try
            {
                Thread.Sleep(60 * 1000);
                if(PendingCancels.ContainsKey(symbol.ToString()))
                    PendingCancels.Remove(symbol.ToString());
            }
            catch (Exception ex)
            {
                DoLog(string.Format("{0} Critical Error removing Pending Cancel for Symbol {1}={2}",
                       "PendilCancelTimeoutThread", symbol.ToString(), ex.Message),Constants.MessageType.Error);
            }
        }

        private CMState CancelRoutingPos(Position rPos, BasePortfImbalancePosition imbPos)
        {
            if (!PendingCancels.ContainsKey(rPos.Symbol))
            {
                lock (PendingCancels)
                {
                    //We have to cancel the position before closing it.
                    PendingCancels.Add(rPos.Symbol, imbPos);
                    new Thread(PendingCancelTimeoutThread).Start(rPos.Symbol);
                }
                CancelPositionWrapper cancelWrapper = new CancelPositionWrapper(rPos, Config);
                return OrderRouter.ProcessMessage(cancelWrapper);
            }
            else
                return CMState.BuildSuccess();
        
        }

        protected CMState RunClose(Position openPos, BaseMonSecurityImbalance secImb, BasePortfImbalancePosition imbPos)
        {
            if (openPos.PosStatus == PositionStatus.Filled)
            {
                if (Security.GetSecurityType(Configuration.SecurityTypes) == SecurityType.FUT)
                    LoadCloseFuturePos(openPos, secImb, imbPos);
                else
                    LoadCloseRegularPos(openPos, secImb, imbPos);
                secImb.Closing = true;
                PositionWrapper posWrapper = new PositionWrapper(imbPos.ClosingPosition, Config);
                return OrderRouter.ProcessMessage(posWrapper);
            }
            else if (openPos.PositionRouting())
            {
                DoLog(string.Format("Cancelling routing pos for symbol {0} before closing (status={1} posId={2})",openPos.Security.Symbol,openPos.PosStatus,openPos.PosId),Constants.MessageType.Information);
                return CancelRoutingPos(openPos, imbPos);
            }
            else
            {
                DoLog(string.Format("{0} Aborting  position on invalid state. Symbol {1} Qty={2} PosStatus={3} PosId={4}", 
                    imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty,openPos.PosStatus.ToString(),openPos.PosId), 
                    Constants.MessageType.Information);
                return CMState.BuildSuccess();
            }
        }

        private bool EvalClosingPositionOnStopLossHit(BaseMonSecurityImbalance secImb)
        {
            if (PortfolioPositions.ContainsKey(secImb.Security.Symbol))
            {
                BasePortfImbalancePosition imbPos = PortfolioPositions[secImb.Security.Symbol];
                
                if(imbPos.EvalStopLossHit(secImb))
                {
                    RunClose(imbPos.OpeningPosition, secImb, imbPos);
                    DoLog(string.Format("{0} Position Closed on stop loss hit. Symbol {1} Qty={2} Imbalance={3} PosId={4}",
                        imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty, secImb.ImbalanceSummary,
                        imbPos.OpeningPosition.PosId), Constants.MessageType.Information);
                    return true;
                }
                else
                    return false;
            }

            return false;
        }

        private void UpdateLastPrice(BaseMonSecurityImbalance secImb,MarketData md)
        {
            if (PortfolioPositions.ContainsKey(secImb.Security.Symbol))
            {
                BasePortfImbalancePosition imbPos = PortfolioPositions[secImb.Security.Symbol];
                if (imbPos.OpeningPosition.PosStatus == PositionStatus.Filled
                    || imbPos.OpeningPosition.PosStatus == PositionStatus.PartiallyFilled)
                {
                    if (imbPos.ClosingPosition == null)
                    {
                        imbPos.LastPrice = md.Trade;
                        SecurityImbalanceManager.PersistSecurityImbalanceTrade(imbPos);
                    }
                }
            }
        
        }

        private void EvalAbortingOpeningPositions(BaseMonSecurityImbalance secImb)
        {
            if (PortfolioPositions.ContainsKey(secImb.Security.Symbol))
            {
                BasePortfImbalancePosition imbPos = PortfolioPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == secImb.Security.Symbol).FirstOrDefault();
            
                if(imbPos.EvalAbortingNewLongPosition(secImb))
                {
                    CancelRoutingPos(imbPos.OpeningPosition, imbPos);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2} Imbalance={3} Pos={4}", imbPos.TradeDirection, 
                         imbPos.OpeningPosition.Security.Symbol, imbPos.Qty,secImb.ImbalanceSummary,imbPos.OpeningPosition.PosId), Constants.MessageType.Information);
                }

                if(imbPos.EvalAbortingNewShortPosition(secImb))
                {
                    CancelRoutingPos(imbPos.OpeningPosition, imbPos);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2} Imbalance={3} Pos={4}", imbPos.TradeDirection, 
                                        imbPos.OpeningPosition.Security.Symbol, imbPos.Qty,secImb.ImbalanceSummary,imbPos.OpeningPosition.PosId), Constants.MessageType.Information);
                }
            }
        }

        private void EvalAbortingClosingPositions(BaseMonSecurityImbalance secImb)
        {
            if (PortfolioPositions.ContainsKey(secImb.Security.Symbol))
            {
                BasePortfImbalancePosition imbPos = PortfolioPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == secImb.Security.Symbol).FirstOrDefault();
            
                if(imbPos.EvalAbortingClosingLongPosition(secImb))
                {
                    CancelRoutingPos(imbPos.ClosingPosition, imbPos);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2} Imbalance={3} Pos={4}", imbPos.TradeDirection,
                                        imbPos.ClosingPosition.Security.Symbol, imbPos.Qty,secImb.ImbalanceSummary,imbPos.ClosingPosition.PosId), Constants.MessageType.Information);
                }

                if (imbPos.EvalAbortingClosingShortPosition(secImb))
                {
                    CancelRoutingPos(imbPos.ClosingPosition, imbPos);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2} Imbalance={3} Pos={4}", imbPos.TradeDirection, 
                                        imbPos.ClosingPosition.Security.Symbol, imbPos.Qty,secImb.ImbalanceSummary,imbPos.ClosingPosition.PosId), Constants.MessageType.Information);
                }
            }
        }

        protected void EvalOpeningPosition(BaseMonSecurityImbalance secImb)
        {
            if (secImb.LongPositionThresholdTriggered())
            {
                BasePortfImbalancePosition imbPos = LoadNewPos(secImb, Side.Buy);
                PositionWrapper posWrapper = new PositionWrapper(imbPos.OpeningPosition, Config);
                PortfolioPositions.Add(imbPos.OpeningPosition.Security.Symbol, imbPos);
                CMState state = OrderRouter.ProcessMessage(posWrapper);
                DoLog(string.Format("{0} Position Opened to market. Symbol {1} CashQty={2} Imbalance={3} PosId={4}", imbPos.TradeDirection,
                    imbPos.OpeningPosition.Security.Symbol, imbPos.OpeningPosition.CashQty, secImb.ImbalanceSummary, imbPos.OpeningPosition.PosId), Constants.MessageType.Information);

            }
            else if (secImb.ShortPositionThresholdTriggered())
            {
                if (!Configuration.OnlyLong)
                {
                    BasePortfImbalancePosition imbPos = LoadNewPos(secImb, Side.Sell);
                    PositionWrapper posWrapper = new PositionWrapper(imbPos.OpeningPosition, Config);
                    PortfolioPositions.Add(imbPos.OpeningPosition.Security.Symbol, imbPos);
                    CMState state = OrderRouter.ProcessMessage(posWrapper);
                    DoLog(
                        string.Format("{0} Position Opened to market. Symbol {1} CashQty={2} Imbalance={3} PosId={4}",
                            imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol,
                            imbPos.OpeningPosition.CashQty, secImb.ImbalanceSummary, imbPos.OpeningPosition.PosId),
                        Constants.MessageType.Information);
                }
                else
                {
                    DoLog(string.Format("SHORT signal for symbol {0} triggered but OnlyLong mode is enabled", secImb.Security.Symbol), Constants.MessageType.Information);
                }
            }
            else {
                DoLog($"===> Imbalance Summary for {secImb.Security.Symbol}: Bid={secImb.ImbalanceCounter.BidSizeImbalance.ToString("0.00")} Ask={secImb.ImbalanceCounter.AskSizeImbalance.ToString("0.00")} ", Constants.MessageType.Information);
            
            }
        }

        protected virtual void EvalClosingPosition(BaseMonSecurityImbalance secImb)
        {
            BasePortfImbalancePosition imbPos = PortfolioPositions[secImb.Security.Symbol];

            if (imbPos.EvalClosingShortPosition(secImb))
            {
                RunClose(imbPos.OpeningPosition, secImb, imbPos);
                DoLog(string.Format("Closing {0} Position on market. Symbol {1} Qty={2} Imbalance={3} PosId={4} ClosingSummary={5}", 
                                            imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty,
                                            secImb.ImbalanceSummary,
                                            imbPos.ClosingPosition!=null? imbPos.ClosingPosition.PosId:"-",
                                            imbPos.ClosingSummary(secImb)), 
                                            Constants.MessageType.Information);
            }
            else if (imbPos.EvalClosingLongPosition(secImb))
            {
                RunClose(imbPos.OpeningPosition, secImb, imbPos);
                DoLog(string.Format("Closing {0} Position on market. Symbol {1} Qty={2}  Imbalance={3} PosId={4} ClosingSummary={5}",
                        imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty,
                        secImb.ImbalanceSummary,
                        imbPos.ClosingPosition != null ? imbPos.ClosingPosition.PosId : null,
                        imbPos.ClosingSummary(secImb)),
                    Constants.MessageType.Information);

            }
         
        }

        protected virtual bool PacingValidations(string symbol)
        {
            return true;
        }

        protected virtual bool IsTradingTime()
        {
            return DateTime.Now < MarketTimer.GetTodayDateTime(Configuration.ClosingTime);
        }

        protected virtual bool InsideTradingWindow()
        {
            return true;
        }

        private void EvalOpeningClosingPositions(BaseMonSecurityImbalance secImb)
        {

            TimeSpan elapsed = DateTime.Now - StartTime;

            if (elapsed.TotalMinutes > (Configuration.BlockSizeInMinutes*Configuration.ActiveBlocks) )
            {
                //Evaluamos no abrir mas posiciones de las deseadas @Configuration.MaxOpenedPositions
                if (PortfolioPositions.Keys.Count < Configuration.MaxOpenedPositions 
                    && !PortfolioPositions.ContainsKey(secImb.Security.Symbol)
                    && IsTradingTime()
                    && InsideTradingWindow()
                    && PacingValidations(secImb.Security.Symbol)
                    )
                {
                    EvalOpeningPosition(secImb);
                }
                else if (PortfolioPositions.ContainsKey(secImb.Security.Symbol) && IsTradingTime())
                {
                    EvalClosingPosition(secImb);
                    EvalClosingPositionOnStopLossHit(secImb);
                    EvalAbortingOpeningPositions(secImb);
                    EvalAbortingClosingPositions(secImb);
                }
            }
            else
                DoLog(string.Format("Waiting for min recovery data to be over. Elapsed {0} minutes",elapsed.TotalMinutes.ToString("0.00")),Constants.MessageType.Information);
        }

        protected void ProcessHistoricalPricesAsync(object param)
        {
            try
            {
                HistoricalPricesWrapper histWrapper = (HistoricalPricesWrapper)param;
                HistoricalPricesDTO hpDto = HistoricalPricesConverter.ConvertHistoricalPrices(histWrapper);


                if (SecurityImbalancesToMonitor.ContainsKey(hpDto.Symbol))
                {
                    BaseMonSecurityImbalance monPos = SecurityImbalancesToMonitor[hpDto.Symbol];
                    DoLog($"{Configuration.Name}-->Loading {hpDto.MarketData.Count} historical candles for symbol {hpDto.Symbol}", Constants.MessageType.Information);
                    foreach (MarketData md in hpDto.MarketData)
                    {
                        monPos.AppendCandleHistorical(md);
                    }

                }
                else
                {
                    DoLog($"{Configuration.Name}-->Ignoring historical prices for non tracked symbol {hpDto.Symbol}", Constants.MessageType.Information);
                }

            }
            catch (Exception ex)
            {
                DoLog($"{Configuration.Name}--> CRITICAL ERROR procesing historical prices:{ex.Message}", Constants.MessageType.Error);

            }
        }

        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            lock (tLock)
            {
                if (SecurityImbalancesToMonitor.ContainsKey(md.Security.Symbol) && Securities!=null)
                {
                    
                    BaseMonSecurityImbalance secImb = SecurityImbalancesToMonitor[md.Security.Symbol];
                    //DoLog(string.Format("Processing MD for imbalance summary: {0}",secImb.ImbalanceSummary),Constants.MessageType.Information);
                    secImb.AppendCandle(md);
                    secImb.ProcessCounters();
                    EvalOpeningClosingPositions(secImb);
                    
                    UpdateLastPrice(secImb, md);
                   
                }
            }

            OrderRouter.ProcessMessage(wrapper);

            return CMState.BuildSuccess();

        }

        private void LoadTradingParameters()
        {
            foreach (Security sec in Securities)
            {
                if (SecurityImbalancesToMonitor.Values.Any(x => x.Security.Symbol == sec.Symbol))
                {
                    BaseMonSecurityImbalance secImb = SecurityImbalancesToMonitor.Values.Where(x => x.Security.Symbol == sec.Symbol).FirstOrDefault();
                    secImb.Security = sec;
                }
            }
        }

        protected void EvalRemoval(BasePortfImbalancePosition imbPos, ExecutionReport report)
        {
            imbPos.CurrentPos().PositionCanceledOrRejected = true;
            imbPos.CurrentPos().PositionCleared = false;
            imbPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
            if (imbPos.IsFirstLeg() && imbPos.OpeningPosition.CumQty == 0)
                PortfolioPositions.Remove(imbPos.OpeningPosition.Security.Symbol);
        
        }

        protected void ProcessOrderRejection(BasePortfImbalancePosition imbPos, ExecutionReport report)
        {
            if (imbPos.CurrentPos().PosStatus == PositionStatus.PendingNew)
            {
                //A position was rejected, we remove it and Log what happened\
                EvalRemoval(imbPos, report);
                DoLog(string.Format("@{0} WARNING - Opening on position rejected for symbol {1} (PosId {3}):{2} ", 
                                            Configuration.Name, imbPos.OpeningPosition.Security.Symbol, report.Text,
                                            imbPos.CurrentPos().PosId), Constants.MessageType.Information);

            }
            else if (imbPos.CurrentPos().PositionRouting()) //OPEN:most probably an update failed--> we do nothing
            {
                DoLog(string.Format("@{0} WARNING-Action on OPEN position rejected for symbol {1} (PosId={3}):{2} ",
                    Configuration.Name, imbPos.OpeningPosition.Security.Symbol, report.Text,
                    imbPos.CurrentPos().PosId), Constants.MessageType.Error);
            }
            else if (imbPos.CurrentPos().PositionNoLongerActive())//CLOSED most probably an update failed--> we do nothing
            {
                //The action that created the rejection state (Ex: the order was canceled or filled) should be
                //handled through the proper execution report
                DoLog(string.Format("@{0} WARNING-Action on CLOSED position rejected for symbol {1} (PosId={3}):{2} ", 
                                            Configuration.Name, imbPos.OpeningPosition.Security.Symbol, report.Text,
                                            imbPos.CurrentPos().PosId), Constants.MessageType.Error);
            }
        
        }

        protected void ProcessOrderCancellation(BasePortfImbalancePosition imbPos,ExecutionReport report)
        {
            lock (PendingCancels)
            {
                //We canceled a position that has to be closed!
                BasePortfImbalancePosition pendImbPos = PendingCancels[report.Order.Symbol];
                DoLog(string.Format("Recv ER for Pending Cancel position for symbol {0}", report.Order.Symbol), 
                            Main.Common.Util.Constants.MessageType.Information);

                if (report.ExecType == ExecType.Canceled)
                {

                    if (pendImbPos.ClosingPosition != null)//I cancelled a Closing Position
                    {

                        //This is what is net open from the position
                        pendImbPos.OpeningPosition.CumQty -= pendImbPos.ClosingPosition.CumQty;
                        DoLog(string.Format("ER Cancelled for Pending Cancel for symbol {0} (posId={2}): We were closing the closing position. New Live Qty={1}", 
                                                   report.Order.Symbol, pendImbPos.OpeningPosition.CumQty,pendImbPos.OpeningPosition.PosId), 
                                                    Main.Common.Util.Constants.MessageType.Information);

                        pendImbPos.ClosingPosition = null;
                        
                    }
                    else 
                    {
                        if (pendImbPos.OpeningPosition.CumQty > 0) //I cancelled an opening position --> I will have Qty=CumQty --> It will be processed in the next MARKET_DATA event
                        {
                            pendImbPos.OpeningPosition.PosStatus = PositionStatus.Filled;
                            DoLog(string.Format("ER Cancelled for Pending Cancel for symbol {0} (PosId={2}): We were opening the position position. Live Qty={1}", 
                                                        report.Order.Symbol, pendImbPos.OpeningPosition.CumQty,pendImbPos.OpeningPosition.PosId), Main.Common.Util.Constants.MessageType.Information);

                        }
                        else
                        {
                            PortfolioPositions.Remove(imbPos.OpeningPosition.Security.Symbol);
                            DoLog(string.Format("ER Cancelled for Pending Cancel for symbol {0} (PosId={1}): We were opening the position position. Live Qty<flat>=0",
                                                      report.Order.Symbol,imbPos.OpeningPosition.PosId), Main.Common.Util.Constants.MessageType.Information);

                            //It was not executed. We can remove the ImbalancePosition
                        }
                    }

                    SecurityImbalancesToMonitor[imbPos.OpeningPosition.Security.Symbol].Closing = false;
                    //Now we can finally close the position
                    PendingCancels.Remove(report.Order.Symbol);
                }
                else//something happened with the cancelation -> we have to try again and log
                {
                    //as the positions stays as Partially Filled we will try again, on and on
                    PendingCancels.Remove(report.Order.Symbol);
                    DoLog(string.Format("ERROR-Problems with cancellation for Pending Cancel for symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}", 
                                        report.Order.Symbol,report.OrdStatus,report.ExecType,report.Text,pendImbPos.CurrentPos().PosId), 
                                        Main.Common.Util.Constants.MessageType.Error);

                }
            }
        }

        protected void AssignMainERParameters(BasePortfImbalancePosition imbPos,ExecutionReport report)
        {
            if (!report.IsCancelationExecutionReport())
            {
                imbPos.CurrentPos().CumQty = report.CumQty;
                imbPos.CurrentPos().LeavesQty = report.LeavesQty;
                imbPos.CurrentPos().AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                imbPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
                imbPos.CurrentPos().ExecutionReports.Add(report);

                if (report.OrdStatus == OrdStatus.Filled)
                {
                    SecurityImbalanceManager.PersistSecurityImbalanceTrade(imbPos);//first leg and second leg

                    if (!imbPos.IsFirstLeg())
                    {
                        DoLog(string.Format("DB-Closing imbalance position for symbol {0} (CumQty={1})",imbPos.OpeningPosition.Security.Symbol,report.CumQty),Constants.MessageType.Information);
                        SecurityImbalancesToMonitor[imbPos.OpeningPosition.Security.Symbol].Closing = false;
                        SecurityImbalancesToMonitor[imbPos.OpeningPosition.Security.Symbol].LastOpenedPosition = imbPos.CurrentPos();
                        SecurityImbalancesToMonitor[imbPos.OpeningPosition.Security.Symbol].LastTradedTime = DateTime.Now;
                        PortfolioPositions.Remove(imbPos.OpeningPosition.Security.Symbol);
                    }
                    else
                    {
                        DoLog(string.Format("DB-Fully opened imbalance {2} position for symbol {0} (CumQty={1})",
                                                    imbPos.OpeningPosition.Security.Symbol,report.CumQty,imbPos.TradeDirection),Constants.MessageType.Information);
                    }
                }
            }
            else
            {
                if (PendingCancels.ContainsKey(report.Order.Symbol))
                {
                    ProcessOrderCancellation(imbPos, report);
                }
                else
                {
                    if (report.OrdStatus == OrdStatus.Rejected)
                    {
                        DoLog(string.Format("Rejected execution report symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}",
                                                report.Order.Symbol, report.OrdStatus, report.ExecType, report.Text,imbPos.CurrentPos().PosId),
                                                Main.Common.Util.Constants.MessageType.Information);
                        ProcessOrderRejection(imbPos, report);
                    }
                    else
                    {
                        DoLog(string.Format("WARNING-Recv ER for symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}",
                                                report.Order.Symbol, report.OrdStatus, report.ExecType, report.Text,imbPos.CurrentPos().PosId),
                                                Main.Common.Util.Constants.MessageType.Information);
                        EvalRemoval(imbPos, report);
                    }
                }
            }
        }

        protected void LogExecutionReport(BasePortfImbalancePosition imbPos, ExecutionReport report)
        {

            DoLog(string.Format("{0} Position {7} ER on Position. Symbol {1} ExecType={7} OrdStatus={8} Qty={2} CumQty={3} LeavesQty={4} AvgPx={5} First Leg={6}",
                          imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty, imbPos.CurrentPos().CumQty, imbPos.CurrentPos().LeavesQty,
                          imbPos.CurrentPos().AvgPx, imbPos.IsFirstLeg(), report.ExecType,report.OrdStatus), Constants.MessageType.Information);

        }

        protected void EvalCancellingOrdersOnStartup(ExecutionReport report)
        {
            if (Configuration.CancelActiveOrdersOnStart && report.IsActiveOrder())
            {
                TimeSpan elapsed = DateTime.Now - StartTime;

                if (elapsed.TotalSeconds > 10)
                {
                    CancelOrderWrapper cxlOrderWrapper = new CancelOrderWrapper(report.Order, Config);
                    OrderRouter.ProcessMessage(cxlOrderWrapper);
                }
                else
                    Configuration.CancelActiveOrdersOnStart = false;
            }
        }
        
        protected void ProcessExecutionReport(object param)
        { 
             Wrapper wrapper = (Wrapper)param;

             try
             {
             
                 lock (tLock)
                 {
                     ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                     EvalCancellingOrdersOnStartup(report);
                     
                     if (PortfolioPositions.ContainsKey(report.Order.Symbol))
                     {
                         BasePortfImbalancePosition imbPos = PortfolioPositions[report.Order.Symbol];
                         AssignMainERParameters(imbPos, report);
                         LogExecutionReport(imbPos, report);
                     }
                 }
             
             }
             catch (Exception e)
             {
                 DoLog(string.Format("Error persisting execution report {0}:{1}-{2}",
                     wrapper != null ? wrapper.ToString() : "?", e.Message,e.StackTrace), Constants.MessageType.Information);
             }
        }

        protected void ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                lock (tLock)
                {
                    SecurityList secList = SecurityListConverter.GetSecurityList(wrapper, Config);
                    Securities = secList.Securities;
                    LoadTradingParameters();
                    DoLog(string.Format("@{0} Saving security list:{1} securities ", Configuration.Name, secList.Securities != null ? secList.Securities.Count : 0), Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0} Error processing security list:{1} ", Configuration.Name, ex.Message), Constants.MessageType.Error);
            }
        
        }

        protected  void DoRequestHistoricalPricesThread(object param)
        {
            try
            {
                int i = 1;

                foreach (string symbol in Configuration.StocksToMonitor)
                {
                    DateTime from = DateTime.Now.AddDays(Configuration.HistoricalPricesPeriod);
                    DateTime to = DateTime.Now.AddDays(1);

                    HistoricalPricesRequestWrapper reqWrapper = new HistoricalPricesRequestWrapper(i, symbol, from, to, CandleInterval.Minute_1);
                    OnMessageRcv(reqWrapper);
                    i++;
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("@BOBDayTurtles - Critical ERROR Requesting Historical Prices:{0}", e.Message), Constants.MessageType.Error);
            }
        }

        #endregion

        #region ICommunicationModule Methods

        //To Process Order Routing Module messages
        protected CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog("Incoming message from order routing: " + wrapper.ToString(), Constants.MessageType.Information);

                if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                {
                    Thread ProcessExecutionReportThread = new Thread(new ParameterizedThreadStart(ProcessExecutionReport));
                    ProcessExecutionReportThread.Start(wrapper);
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST_REQUEST)
                {
                    OnMessageRcv(wrapper);
                }
               
                //else if (wrapper.GetAction() == Actions.NEW_POSITION_CANCELED)
                //{
                //    ProcessNewPositionCanceled(wrapper);
                //}

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        public CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    //DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);

                    return ProcessMarketData(wrapper);
                }
                else if (wrapper.GetAction() == Actions.HISTORICAL_PRICES)
                {
                    //DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    (new Thread(ProcessHistoricalPricesAsync)).Start(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    ProcessSecurityList(wrapper);
                    return OrderRouter.ProcessMessage(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Configuration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + Configuration.Name + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public virtual bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;
                StartTime = DateTime.Now;
                LastCounterResetTime = StartTime;

                if (ConfigLoader.LoadConfig(this, configFile))
                {
                    tLock = new object();
                    SecurityImbalanceManager = new SecurityImbalanceManager(Configuration.ConnectionString);
                    ImbalancePositionManager = new ImbalancePositionManager(Configuration.ConnectionString, Configuration);
                    CustomImbalanceConfigManager = new CustomImbalanceConfigManager(Configuration.ConnectionString);
                    SecurityImbalancesToMonitor = new Dictionary<string, BaseMonSecurityImbalance>();
                    PortfolioPositions = new Dictionary<string, BasePortfImbalancePosition>();
                    PendingCancels = new Dictionary<string, BasePortfImbalancePosition>();
                    MarketDataConverter = new MarketDataConverter();
                    ExecutionReportConverter = new ExecutionReportConverter();
                    SecurityListConverter = new SecurityListConverter();
                    Securities = new List<Security>();

                    NextPosId = 1;

                    (new Thread(new ParameterizedThreadStart(DoRequestHistoricalPricesThread))).Start();
                    
                    LoadMonitorsAndRequestMarketData();

                    InitializeCommunicationModules(pOnLogMsg);

                    SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities, null);
                    OnMessageRcv(slWrapper);

                    ResetEveryNMinutesThread = new Thread(ResetEveryNMinutes);
                    ResetEveryNMinutesThread.Start();
                    
                    CloseOnTradingTimeOffThread = new Thread(CloseOnTradingTimeOff);
                    CloseOnTradingTimeOffThread.Start();

                    LoadPreviousImbalancePositions();
                    
                    RequestOrderStatuses();
                    //SecImbalancePersistanceThread = new Thread(ImbalancePersistanceThread);
                    //SecImbalancePersistanceThread.Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}

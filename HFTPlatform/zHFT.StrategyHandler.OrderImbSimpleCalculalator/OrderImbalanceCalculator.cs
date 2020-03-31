using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using zHFT.OrderImbSimpleCalculator.BusinessEntities;
using zHFT.OrderImbSimpleCalculator.Common.Configuration;
using zHFT.OrderImbSimpleCalculator.Common.Enums;
using zHFT.OrderImbSimpleCalculator.DataAccessLayer;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;


namespace zHFT.StrategyHandler.OrderImbSimpleCalculator
{
    public class OrderImbalanceCalculator : ICommunicationModule,ILogger
    {
        #region Protected Attributes

        protected int NextPosId { get; set; }

        protected ICommunicationModule OrderRouter { get; set; }

        protected Dictionary<string, ImbalancePosition> ImbalancePositions { get; set; }

        protected Dictionary<string, ImbalancePosition> PendingCancelPosClosing { get; set; }

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

        protected Dictionary<string, SecurityImbalance> SecurityImbalancesToMonitor { get; set; }

        protected object tLock { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected SecurityImbalanceManager SecurityImbalanceManager { get; set; }

        protected ImbalancePositionManager ImbalancePositionManager { get; set; }

        protected DateTime? LastPersistanceTime {get;set;}

        protected DateTime StartTime { get; set; }

        protected DateTime LastCounterResetTime { get; set; }

        protected Thread SecImbalancePersistanceThread { get; set; }

        protected Thread ResetEveryNMinutesThread { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }

        protected List<Security> Securities { get; set; }

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

        public  void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration().GetConfiguration<zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration>(configFile, noValueFields);
        }

       
        #endregion

        #region Private Methods

        private void LoadPreviousImbalancePositions()
        {
            List<ImbalancePosition> imbalancePositions = ImbalancePositionManager.GetImbalancePositions(Configuration.Name, true);

            imbalancePositions.ForEach(x => ImbalancePositions.Add(x.OpeningPosition.Security.Symbol, x));
        
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

        private void ResetEveryNMinutes(object param)
        {
            if (Configuration.ResetEveryNMinutes == 0)
                return;

            while (true)
            {
                TimeSpan elapsed = DateTime.Now - LastCounterResetTime;

                if (elapsed.TotalMinutes > Configuration.ResetEveryNMinutes)
                {
                    lock (tLock)
                    {
                        foreach (SecurityImbalance secImb in SecurityImbalancesToMonitor.Values)
                        {
                            secImb.ResetCounters(Configuration.ResetEveryNMinutes);
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
                            foreach (SecurityImbalance secImb in SecurityImbalancesToMonitor.Values)
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

        private void LoadMonitorsAndRequestMarketData()
        {
            Thread.Sleep(5000);
            foreach (string symbol in Configuration.StocksToMonitor)
            {
                Security sec = new Security()
                {
                    Symbol = symbol,
                    SecType = Security.GetSecurityType(Configuration.SecurityTypes),
                    Currency = Configuration.Currency,
                    Exchange = Configuration.Exchange
                };

                SecurityImbalance secImbalance = new SecurityImbalance()
                {
                    Security = sec,
                    DecimalRounding = Configuration.DecimalRounding,
                    TradeImpacts = new List<zHFT.OrderImbSimpleCalculator.BusinesEntities.TradeImpact>()
                };

                //1- We add the current security to monitor
                SecurityImbalancesToMonitor.Add(symbol, secImbalance);

                Securities.Add(sec);//So far, this is all wehave regarding the Securities

                //2- We request market data

                MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter,sec, SubscriptionRequestType.SnapshotAndUpdates);
                MarketDataRequestCounter++;
                OnMessageRcv(wrapper);
            }
        }

        private ImbalancePosition LoadNewRegularPos(SecurityImbalance secImb, Side side)
        {

            Position pos = new Position()
            {
                Security = new Security()
                {
                    Symbol = secImb.Security.Symbol,
                    MarketData = null,
                    Currency = Configuration.Currency,
                    SecType = Security.GetSecurityType(Configuration.SecurityTypes)
                },
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Configuration.PositionSizeInCash,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                AccountId = "TEST",
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            return new ImbalancePosition()
            {
                StrategyName = Configuration.Name,
                OpeningDate = DateTime.Now,
                OpeningPosition = pos,
                OpeningImbalance = secImb,
                FeeTypePerTrade = Configuration.FeeTypePerTrade,
                FeeValuePerTrade = Configuration.FeeValuePerTrade
            };
        
        }

        private FutureImbalancePosition LoadNewFuturePos(SecurityImbalance secImb, Side side)
        {
            Position pos = new Position()
            {
                Security = new Security()
                {
                    Symbol = secImb.Security.Symbol,
                    MarketData = null,
                    Currency = Configuration.Currency,
                    SecType = Security.GetSecurityType(Configuration.SecurityTypes)
                },
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Configuration.PositionSizeInCash.HasValue ? (double?)Configuration.PositionSizeInCash.Value : null,
                Qty = Configuration.PositionSizeInContracts.HasValue ? (double?)Configuration.PositionSizeInContracts.Value : null,
                QuantityType = Configuration.PositionSizeInContracts.HasValue ? QuantityType.CONTRACTS : QuantityType.OTHER,//Tenemos un monto en dólars, pero es el ruteador de ordenes el que decide a cuantos contratos equivale
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                AccountId = "TEST",
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

        private ImbalancePosition LoadNewPos(SecurityImbalance secImb, Side side)
        {

            if (Security.GetSecurityType(Configuration.SecurityTypes) == SecurityType.FUT)
                return LoadNewFuturePos(secImb, side);
            else
                return LoadNewRegularPos(secImb, side);
        }

        private void LoadCloseFuturePos(Position openPos, SecurityImbalance secImb, ImbalancePosition imbPos)
        {
            Position pos = new Position()
            {
                Security = new Security()
                {
                    Symbol = secImb.Security.Symbol,
                    MarketData = null,
                    Currency = Configuration.Currency,
                    SecType = Security.GetSecurityType(Configuration.SecurityTypes)
                },
                Side = openPos.Side == Side.Buy ? Side.Sell : Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.CONTRACTS ,//Tenemos un monto en dólars, pero es el ruteador de ordenes el que decide a cuantos contratos equivale
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                AccountId = "TEST",
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            imbPos.ClosingPosition = pos;
            imbPos.ClosingDate = DateTime.Now;
            imbPos.ClosingImbalance = secImb;
        
        }

        private void LoadCloseRegularPos(Position openPos, SecurityImbalance secImb, ImbalancePosition imbPos)
        {
            Position pos = new Position()
            {
                Security = new Security()
                {
                    Symbol = openPos.Security.Symbol,
                    MarketData = null,
                    Currency = Configuration.Currency,
                    SecType = SecurityType.CS
                },
                Side = openPos.Side == Side.Buy ? Side.Sell : Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.SHARES,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = "TEST"
            };


            pos.PositionCleared = true;
            pos.LoadPosId(NextPosId);
            NextPosId++;

            imbPos.ClosingPosition = pos;
            imbPos.ClosingDate = DateTime.Now;
            imbPos.ClosingImbalance = secImb;
        
        }

        private CMState LoadClosePos(Position openPos, SecurityImbalance secImb, ImbalancePosition imbPos)
        {
            if (openPos.PosStatus == PositionStatus.Filled)
            {
                if (Security.GetSecurityType(Configuration.SecurityTypes) == SecurityType.FUT)
                    LoadCloseFuturePos(openPos, secImb, imbPos);
                else
                    LoadCloseRegularPos(openPos, secImb, imbPos);

                PositionWrapper posWrapper = new PositionWrapper(imbPos.ClosingPosition, Config);
                return OrderRouter.ProcessMessage(posWrapper);
            }
            else if (openPos.PositionActive())
            {
                if (!PendingCancelPosClosing.ContainsKey(openPos.Symbol))
                {
                    lock (PendingCancelPosClosing)
                    {
                        //We have to cancel the position before closing it.
                        PendingCancelPosClosing.Add(openPos.Symbol, imbPos);
                    }
                    CancelPositionWrapper cancelWrapper = new CancelPositionWrapper(openPos, Config);
                    return OrderRouter.ProcessMessage(cancelWrapper);
                }
                else
                    return CMState.BuildSuccess();
            }
            else
            {
                DoLog(string.Format("{0} Aborting  position on invalid state. Symbol {1} Qty={2} PosStatus={3}", 
                    imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty,openPos.PosStatus.ToString()), 
                    Constants.MessageType.Information);
                return CMState.BuildSuccess();
            }
        }

        private bool EvalClosingPositionOnStopLossHit(SecurityImbalance secImb)
        {
            if (ImbalancePositions.ContainsKey(secImb.Security.Symbol))
            {
                ImbalancePosition imbPos = ImbalancePositions[secImb.Security.Symbol];
                return imbPos.EvalStopLossHit(secImb);
            }

            return false;
        }

        private void UpdateLastPrice(SecurityImbalance secImb,MarketData md)
        {
            if (ImbalancePositions.ContainsKey(secImb.Security.Symbol))
            {
                ImbalancePosition imbPos = ImbalancePositions[secImb.Security.Symbol];
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

        private void EvalAbortingNewPositions(SecurityImbalance secImb)
        {
            if (ImbalancePositions.ContainsKey(secImb.Security.Symbol))
            {
                ImbalancePosition imbPos = ImbalancePositions.Values.Where(x => x.OpeningPosition.Security.Symbol == secImb.Security.Symbol).FirstOrDefault();
            
                if(imbPos.EvalAbortingNewLongPosition(secImb,Configuration.PositionOpeningImbalanceThreshold))
                {
                    CancelPositionWrapper cancelPositionWrapper = new CancelPositionWrapper(imbPos.OpeningPosition,Configuration);
                    OrderRouter.ProcessMessage(cancelPositionWrapper);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);

                }

                if(imbPos.EvalAbortingNewShortPosition(secImb,Configuration.PositionOpeningImbalanceThreshold))
                {
                    CancelPositionWrapper cancelPositionWrapper = new CancelPositionWrapper(imbPos.OpeningPosition,Configuration);
                    OrderRouter.ProcessMessage(cancelPositionWrapper);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);
                }
            }
        }

        private void EvalAbortingClosingPositions(SecurityImbalance secImb)
        {
            if (ImbalancePositions.ContainsKey(secImb.Security.Symbol))
            {
                ImbalancePosition imbPos = ImbalancePositions.Values.Where(x => x.OpeningPosition.Security.Symbol == secImb.Security.Symbol).FirstOrDefault();
            
                if(imbPos.EvalAbortingClosingLongPosition(secImb,Configuration.PositionOpeningImbalanceMaxThreshold))
                {
                    CancelPositionWrapper cancelPositionWrapper = new CancelPositionWrapper(imbPos.OpeningPosition,Configuration);
                    OrderRouter.ProcessMessage(cancelPositionWrapper);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);

                }

                if (imbPos.EvalAbortingClosingShortPosition(secImb, Configuration.PositionOpeningImbalanceMaxThreshold))
                {
                    CancelPositionWrapper cancelPositionWrapper = new CancelPositionWrapper(imbPos.OpeningPosition,Configuration);
                    OrderRouter.ProcessMessage(cancelPositionWrapper);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);
                }
            }
        }

        private void EvalOpeningPosition(SecurityImbalance secImb)
        {
            if (secImb.LongPositionThresholdTriggered(Configuration.PositionOpeningImbalanceThreshold))
            {
                ImbalancePosition imbPos = LoadNewPos(secImb, Side.Buy);
                PositionWrapper posWrapper = new PositionWrapper(imbPos.OpeningPosition, Config);
                ImbalancePositions.Add(imbPos.OpeningPosition.Security.Symbol, imbPos);
                CMState state = OrderRouter.ProcessMessage(posWrapper);
                DoLog(string.Format("{0} Position Opened to market. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);



            }
            else if (secImb.ShortPositionThresholdTriggered(Configuration.PositionOpeningImbalanceThreshold))
            {
                
                ImbalancePosition imbPos = LoadNewPos(secImb, Side.Sell);
                PositionWrapper posWrapper = new PositionWrapper(imbPos.OpeningPosition, Config);
                ImbalancePositions.Add(imbPos.OpeningPosition.Security.Symbol, imbPos);
                CMState state = OrderRouter.ProcessMessage(posWrapper);
                DoLog(string.Format("{0} Position Opened to market. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);
            }
        }

        private void EvalClosingPosition(SecurityImbalance secImb)
        {
            ImbalancePosition imbPos = ImbalancePositions[secImb.Security.Symbol];

            if (imbPos.EvalClosingShortPosition(secImb, Configuration.PositionOpeningImbalanceMaxThreshold))
            {
                LoadClosePos(imbPos.OpeningPosition, secImb, imbPos);
                DoLog(string.Format("{0} Position Closed on market. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);
            }
            else if (imbPos.EvalClosingLongPosition(secImb, Configuration.PositionOpeningImbalanceMaxThreshold))
            {
                LoadClosePos(imbPos.OpeningPosition, secImb, imbPos);
                DoLog(string.Format("{0} Position Closed on market. Symbol {1} Qty={2}", imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty), Constants.MessageType.Information);

            }
        }

        private void EvalOpeningClosingPositions(SecurityImbalance secImb)
        {

            TimeSpan elapsed = DateTime.Now - StartTime;

            if (elapsed.TotalMinutes > Configuration.WaitingTimeBeforeOpeningPositions )
            {
                //Evaluamos no abrir mas posiciones de las deseadas @Configuration.MaxOpenedPositions
                if (ImbalancePositions.Keys.Count < Configuration.MaxOpenedPositions && !ImbalancePositions.ContainsKey(secImb.Security.Symbol))
                {
                    EvalOpeningPosition(secImb);
                }
                else if (ImbalancePositions.ContainsKey(secImb.Security.Symbol))
                {
                    EvalClosingPosition(secImb);
                    EvalAbortingNewPositions(secImb);
                    EvalAbortingClosingPositions(secImb);
                }
            }
        }

        private CMState ProcessMarketData(Wrapper wrapper)
        {
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            lock (tLock)
            {
                if (SecurityImbalancesToMonitor.ContainsKey(md.Security.Symbol) && Securities!=null)
                {
                    
                    SecurityImbalance secImb = SecurityImbalancesToMonitor[md.Security.Symbol];
                    DoLog(string.Format("Processing MD for imbalance summary: {0}",secImb.ImbalanceSummary),Constants.MessageType.Information);
                    secImb.Security.MarketData = md;
                    secImb.ProcessCounters();
                    EvalOpeningClosingPositions(secImb);
                    EvalClosingPositionOnStopLossHit(secImb);
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
                    SecurityImbalance secImb = SecurityImbalancesToMonitor.Values.Where(x => x.Security.Symbol == sec.Symbol).FirstOrDefault();
                    secImb.Security = sec;
                }
            }
        }

        protected void RemovePosition(ImbalancePosition imbPos, ExecutionReport report)
        {
            imbPos.CurrentPos().PositionCanceledOrRejected = true;
            imbPos.CurrentPos().PositionCleared = false;
            imbPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
            if (imbPos.IsFirstLeg() && imbPos.OpeningPosition.CumQty == 0)
                ImbalancePositions.Remove(imbPos.OpeningPosition.Security.Symbol);
        
        }

        protected void ProcessOrderRejection(ImbalancePosition imbPos, ExecutionReport report)
        {
            if (imbPos.CurrentPos().PosStatus == PositionStatus.PendingNew)
            {
                //An opening position was rejected, we remove it and Log what happened\
                RemovePosition(imbPos, report);
                DoLog(string.Format("@{0} Opening on position rejected for symbol{1}:{2} ", Configuration.Name, imbPos.OpeningPosition.Security.Symbol, report.Text), Constants.MessageType.Information);

            }
            else if (imbPos.CurrentPos().PositionActive())//OPEN:most probably an update failed--> we do nothing
                DoLog(string.Format("@{0} Action on OPEN position rejected for symbol{1}:{2} ", Configuration.Name, imbPos.OpeningPosition.Security.Symbol, report.Text), Constants.MessageType.Error);
            else if (imbPos.CurrentPos().PositionNoLongerActive())//CLOSED most probably an update failed--> we do nothing
            {
                //The action that created the rejection state (Ex: the order was canceled or filled) should be
                //handled through the proper execution report
                DoLog(string.Format("@{0} Action on CLOSED position rejected for symbol{1}:{2} ", Configuration.Name, imbPos.OpeningPosition.Security.Symbol, report.Text), Constants.MessageType.Error);
            }
        
        }

        protected void AssignMainERParameters(ImbalancePosition imbPos,ExecutionReport report, bool activePos)
        {
            if (activePos)
            {
                imbPos.CurrentPos().CumQty = report.CumQty;
                imbPos.CurrentPos().LeavesQty = report.LeavesQty;
                imbPos.CurrentPos().AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                imbPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
                imbPos.CurrentPos().ExecutionReports.Add(report);

                if (report.OrdStatus == OrdStatus.Filled || report.OrdStatus == OrdStatus.PartiallyFilled)
                {
                    SecurityImbalanceManager.PersistSecurityImbalanceTrade(imbPos);//first leg and second leg

                    if (!imbPos.IsFirstLeg())
                        ImbalancePositions.Remove(imbPos.OpeningPosition.Security.Symbol);
                }
            }
            else
            {
                if (PendingCancelPosClosing.ContainsKey(report.Order.Symbol))
                {
                    lock (PendingCancelPosClosing)
                    {
                        //We canceled a position that has to be closed!
                        ImbalancePosition pendImbPos = PendingCancelPosClosing[report.Order.Symbol];

                        if (report.ExecType == ExecType.Canceled)
                        {
                            //Now we can finally close the position
                            pendImbPos.OpeningPosition.PosStatus = PositionStatus.Filled;//Now it will be processed with the next imbalance
                            PendingCancelPosClosing.Remove(report.Order.Symbol);
                        }
                        else//something happened with the cancelation -> we have to try again and log
                        {
                            //as the positions stays as Partially Filled we will try again, on and on
                            PendingCancelPosClosing.Remove(report.Order.Symbol);
                        }
                    }
                }
                else
                {
                    if (report.OrdStatus == OrdStatus.Rejected)
                    {
                        ProcessOrderRejection(imbPos, report);
                    }
                    else
                    {
                        RemovePosition(imbPos, report);
                    }
                }
            }
        }

        protected void LogExecutionReport(ImbalancePosition imbPos, ExecutionReport report)
        {

            DoLog(string.Format("{0} Position {7} ER on Position. Symbol {1} Qty={2} CymQty={3} LeavesQty={4} AvgPx-{5} First Leg={6}",
                          imbPos.TradeDirection, imbPos.OpeningPosition.Security.Symbol, imbPos.Qty, imbPos.CurrentPos().CumQty, imbPos.CurrentPos().LeavesQty,
                          imbPos.CurrentPos().AvgPx, imbPos.IsFirstLeg(), report.ExecType), Constants.MessageType.Information);

        }

        protected void ProcessExecutionReport(object param)
        { 
             Wrapper wrapper = (Wrapper)param;
             lock (tLock)
             {
                 ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                 if (ImbalancePositions.ContainsKey(report.Order.Symbol))
                 {
                     ImbalancePosition imbPos = ImbalancePositions[report.Order.Symbol];
                     AssignMainERParameters(imbPos, report, !report.IsCancelationExecutionReport());
                     LogExecutionReport(imbPos, report);
                 }
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
                    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);

                    return ProcessMarketData(wrapper);
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

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
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
                    SecurityImbalancesToMonitor = new Dictionary<string, SecurityImbalance>();
                    ImbalancePositions = new Dictionary<string, ImbalancePosition>();
                    PendingCancelPosClosing = new Dictionary<string, ImbalancePosition>();
                    MarketDataConverter = new MarketDataConverter();
                    ExecutionReportConverter = new ExecutionReportConverter();
                    SecurityListConverter = new SecurityListConverter();
                    Securities = new List<Security>();

                    NextPosId = 1;

                    LoadMonitorsAndRequestMarketData();

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

                    SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities, null);
                    OnMessageRcv(slWrapper);

                    ResetEveryNMinutesThread = new Thread(ResetEveryNMinutes);
                    ResetEveryNMinutesThread.Start();

                    LoadPreviousImbalancePositions();
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

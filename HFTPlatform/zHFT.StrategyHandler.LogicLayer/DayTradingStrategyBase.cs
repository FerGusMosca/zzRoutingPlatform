using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Configuration;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.OrderImbSimpleCalculator.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.DTO;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;
using MarketTimer = zHFT.Main.Common.Util.MarketTimer;

namespace zHFT.StrategyHandler.LogicLayer
{
    public abstract class DayTradingStrategyBase : ICommunicationModule, ILogger
    {
        #region Protecte Attributes

        protected int NextPosId { get; set; }

        protected ICommunicationModule IncomingModule { get; set; }

        protected ICommunicationModule OrderRouter { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        public string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected BaseStrategyConfiguration Config { get; set; }

        protected object tLock { get; set; }

        protected object tPersistLock { get; set; }
        
        protected MarketDataConverter MarketDataConverter { get; set; }

        protected DateTime? LastPersistanceTime { get; set; }

        protected DateTime StartTime { get; set; }

        protected DateTime? EndTime { get; set; }

        protected bool TradingStarted { get; set; }//Set to true when the first candle arrives

        protected DateTime LastCounterResetTime { get; set; }

        protected Thread CloseOnTradingTimeOffThread { get; set; }

        protected Thread ResetEveryNMinutesThread { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }

        protected List<Security> Securities { get; set; }

        protected Dictionary<string, PortfolioPosition> PendingCancels { get; set; }

        protected ConcurrentDictionary<string, PortfolioPosition> PortfolioPositions { get; set; }

        protected Dictionary<string, MonitoringPosition> MonitorPositions { get; set; }

        protected PositionIdTranslator PositionIdTranslator { get; set; }

        protected object tSynchronizationLock { get; set; }

        #endregion

        #region Load Methods

        public void DoLog(string msg, zHFT.Main.Common.Util.Constants.MessageType type)
        {
            if(OnLogMsg!=null)
                OnLogMsg(string.Format("{0}", msg), type);
        }

        public virtual void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            throw new NotImplementedException();
        }


        public void DoLoadConfig<T>(string configFile, List<string> noValFlds)
        {
            ConfigLoader.GetConfiguration<T>(this, configFile, noValFlds);
        }

        #endregion

        #region Protected Abstract Methods


        public abstract void InitializeManagers(string connStr);

        protected abstract void ProcessHistoricalPrices(object pWrapper);
        
        protected abstract  void ProcessMarketData(object pWrapper);

        protected abstract void DoPersist(PortfolioPosition trdPos);

        protected abstract void ResetEveryNMinutes(object param);

        protected abstract void LoadPreviousTradingPositions();

        protected abstract PortfolioPosition DoOpenTradingRegularPos(Position pos, MonitoringPosition portfPos);

        protected abstract PortfolioPosition DoOpenTradingFuturePos(Position pos, MonitoringPosition portfPos);

        #endregion

        #region Protected Methods


        protected HistoricalPricesDTO LoadHistoricalPrices(HistoricalPricesWrapper hpWrapper)
        {

            HistoricalPricesDTO hpDto = HistoricalPricesConverter.ConvertHistoricalPrices(hpWrapper);

            if (MonitorPositions.ContainsKey(hpDto.Symbol))
            {
                MonitoringPosition monPortfPos = MonitorPositions[hpDto.Symbol];

                foreach (MarketData md in hpDto.MarketData)
                {

                    if (MonitorPositions.ContainsKey(md.Security.Symbol) && Securities != null)
                    {
                        monPortfPos.AppendCandleHistorical(md);
                    }
                }
            }

            return hpDto;
        }

        protected virtual void DoRequestHistoricalPricesThread(object param) { }

        protected virtual void DoRequestSecurityListThread(object param) {

            try
            {

                SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities, null);
                OnMessageRcv(slWrapper);
            }
            catch (Exception ex)
            {

                DoLog($"CRITICAL ERROR Requsting Security List:{ex.Message}", Constants.MessageType.Error);
            }
        }

        protected void LoadTradingParameters()
        {
            foreach (Security sec in Securities)
            {
                if (MonitorPositions.Values.Any(x => x.Security.Symbol == sec.Symbol))
                {
                    MonitoringPosition protfPos = MonitorPositions.Values.Where(x => x.Security.Symbol == sec.Symbol).FirstOrDefault();
                    protfPos.Security = sec;
                }
            }
        }
        
        protected void EvalRemoval(PortfolioPosition trdPos, ExecutionReport report)
        {
            trdPos.CurrentPos().PositionCanceledOrRejected = true;
            trdPos.CurrentPos().PositionCleared = false;
            trdPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
            if (trdPos.IsFirstLeg() )
            {
                if (trdPos.OpeningPosition.CumQty == 0)
                    PortfolioPositions.TryRemove(trdPos.OpeningPosition.Security.Symbol,out _);
                else //it was filled and will have to be treated as such
                    trdPos.CurrentPos().SetPositionStatusFromExecutionStatus(OrdStatus.Filled); 

            }
            else
            {
                if (trdPos.ClosingPosition.CumQty == 0)
                    PortfolioPositions.TryRemove(trdPos.ClosingPosition.Security.Symbol, out _);
                else //it was filled and will have to be treated as such
                    trdPos.CurrentPos().SetPositionStatusFromExecutionStatus(OrdStatus.Filled);


            }
        
        }
        
        protected void LoadCloseFuturePos(Position openPos,MonitoringPosition monPos, PortfolioPosition trdPos)
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
                Security = monPos.Security,
                Side = openPos.Side == Side.Buy ? Side.Sell : Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.CONTRACTS ,//Tenemos un monto en dólars, pero es el ruteador de ordenes el que decide a cuantos contratos equivale
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Config.StopLossForOpenPositionPct),
                AccountId = Config.Account,
                TriggerPrice=monPos.GetLastTriggerPrice()
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            trdPos.ClosingPosition = pos;
            trdPos.ClosingDate = DateTimeManager.Now;
            trdPos.DoCloseTradingPosition(trdPos);
        
        }
        
        private PortfolioPosition LoadNewRegularPos(MonitoringPosition monPos, Side side)
        {
            Position trdPos = new Position()
            {
                Security = monPos.Security,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Config.PositionSizeInCash,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Config.StopLossForOpenPositionPct),
                AccountId = Config.Account,
                TriggerPrice=monPos.GetLastTriggerPrice()
            };

            //pos.LoadPosId(NextPosId);
            trdPos.LoadPosGuid(PositionIdTranslator.GetNextGuidPosId());
            NextPosId++;

            return DoOpenTradingRegularPos(trdPos, monPos);
        
        }
        
        private PortfolioPosition LoadNewFuturePos(MonitoringPosition monfPos, Side side)
        {
            Position pos = new Position()
            {
                Security = monfPos.Security,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Config.PositionSizeInCash.HasValue ? (double?)Config.PositionSizeInCash.Value : null,
                Qty = Config.PositionSizeInContracts.HasValue ? (double?)Config.PositionSizeInContracts.Value : null,
                QuantityType = Config.PositionSizeInContracts.HasValue ? QuantityType.CONTRACTS : QuantityType.OTHER,//Tenemos un monto en dólars, pero es el ruteador de ordenes el que decide a cuantos contratos equivale
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                StopLossPct = Convert.ToDouble(Config.StopLossForOpenPositionPct),
                AccountId = Config.Account,
                TriggerPrice=monfPos.GetLastTriggerPrice()
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;
            
            return DoOpenTradingRegularPos(pos, monfPos);
        }
        
        protected PortfolioPosition LoadNewPos(MonitoringPosition portPos, Side side)
        {

            if (Security.GetSecurityType(Config.SecurityTypes) == SecurityType.FUT)
                return LoadNewFuturePos(portPos, side);
            else
                return LoadNewRegularPos(portPos, side);
        }
        
        protected void AssignMainERParameters(PortfolioPosition portfPos,ExecutionReport report)
        {
            if (!report.IsCancelationExecutionReport())
            {
                portfPos.CurrentPos().CumQty = report.CumQty;
                portfPos.CurrentPos().LeavesQty = report.LeavesQty;
                portfPos.CurrentPos().AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                portfPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
                portfPos.CurrentPos().ExecutionReports.Add(report);

                if (report.OrdStatus == OrdStatus.Filled)
                {
                    DoLog($"Persisting ER for symbol {report.Order.Symbol} w/Status ={report.OrdStatus} (AvgPx={report.AvgPx}) --> FirstLeg?={portfPos}", Constants.MessageType.Information);

                    if (!portfPos.IsFirstLeg())
                    {
                        DoLog(string.Format("DB-Closing position for symbol {0} (CumQty={1})", portfPos.OpeningPosition.Security.Symbol, report.CumQty),Constants.MessageType.Information);
                        MonitorPositions[portfPos.OpeningPosition.Security.Symbol].Closing = false;
                        PortfolioPositions.TryRemove(portfPos.OpeningPosition.Security.Symbol, out _);
                    }
                    else
                    {
                        DoLog(string.Format("DB-Fully opened {2} position for symbol {0} (CumQty={1})",portfPos.OpeningPosition.Security.Symbol,report.CumQty,portfPos.TradeDirection),Constants.MessageType.Information);
                    }
                }
            }
            else
            {
                if (PendingCancels.ContainsKey(report.Order.Symbol))
                {
                    ProcessOrderCancellation(portfPos, report);
                }
                else
                {
                    if (report.OrdStatus == OrdStatus.Rejected)
                    {
                        DoLog(string.Format("Rejected execution report symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}",
                                                report.Order.Symbol, report.OrdStatus, report.ExecType, report.Text,portfPos.CurrentPos().PosId),
                                                Main.Common.Util.Constants.MessageType.Information);
                        ProcessOrderRejection(portfPos, report);
                    }
                    else
                    {
                        DoLog(string.Format("WARNING-Recv ER for symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}",
                                                report.Order.Symbol, report.OrdStatus, report.ExecType, report.Text,portfPos.CurrentPos().PosId),
                                                Main.Common.Util.Constants.MessageType.Information);
                        EvalRemoval(portfPos, report);
                    }
                }
            }

            if (!(portfPos.IsFirstLeg() && portfPos.CurrentPos().CumQty == 0))
            {

                Thread persitThread = new Thread(new ParameterizedThreadStart(DoPersistThread));
                persitThread.Start(portfPos);
            }

        }
        
        private void LoadCloseRegularPos(Position openPos, MonitoringPosition monPos, PortfolioPosition trdPos)
        {
            Position closingPos = new Position()
            {
//                Security = new Security()
//                {
//                    Symbol = openPos.Security.Symbol,
//                    MarketData = null,
//                    Currency = Configuration.Currency,
//                    SecType = SecurityType.CS
//                },
                Security = monPos.Security,
                Side = openPos.Side == Side.Buy ? Side.Sell : Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.SHARES,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = Config.Account,
                TriggerPrice=monPos.GetLastTriggerPrice()
            };


            closingPos.PositionCleared = true;
            closingPos.LoadPosGuid(PositionIdTranslator.GetNextGuidPosId());
            //closingPos.LoadPosId(NextPosId);
            NextPosId++;

            trdPos.ClosingPosition = closingPos;
            trdPos.ClosingDate = DateTimeManager.Now;
            trdPos.DoCloseTradingPosition(trdPos);
        }
        
        protected void EvalCancellingOrdersOnStartup(ExecutionReport report)
        {
            if (Config.CancelActiveOrdersOnStart && report.IsActiveOrder())
            {
                TimeSpan elapsed = DateTimeManager.Now - StartTime;

                if (elapsed.TotalSeconds > 10)
                {
                    CancelOrderWrapper cxlOrderWrapper = new CancelOrderWrapper(report.Order, Config);
                    OrderRouter.ProcessMessage(cxlOrderWrapper);
                }
                else
                    Config.CancelActiveOrdersOnStart = false;
            }
        }
        
        protected virtual void ProcessExecutionReport(object param)
        { 
            Wrapper wrapper = (Wrapper)param;

            try
            {
             
                //lock (tLock) --> //TODO--. Eval replace by concurrent dict
                //{
                ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                EvalCancellingOrdersOnStartup(report);
                DoLog($"Recv ER for symbol {report.Order.Symbol} w/Status ={report.OrdStatus})", Constants.MessageType.Information);
                if (PortfolioPositions.ContainsKey(report.Order.Symbol))
                {
                    PortfolioPosition trdPos = PortfolioPositions[report.Order.Symbol];
                    AssignMainERParameters(trdPos, report);
                    LogExecutionReport(trdPos, report);
                }
                //}
             
            }
            catch (Exception e)
            {
                DoLog(string.Format("CRITICAL ERROR persisting execution report {0}:{1}",wrapper.ToString(),e.Message),Constants.MessageType.Information);
            }
        }
        
        protected void RequestOrderStatuses()
        {
            OrderMassStatusRequestWrapper omsReq = new OrderMassStatusRequestWrapper();
            
            OrderRouter.ProcessMessage(omsReq);
        }
        
        protected void PendingCancelTimeoutThread(object symbol)
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

        protected CMState CancelRoutingPos(Position routPos, PortfolioPosition tradingPos)
        {
            if (!PendingCancels.ContainsKey(routPos.Symbol))
            {
                lock (PendingCancels)
                {
                    //We have to cancel the position before closing it.
                    PendingCancels.Add(routPos.Symbol, tradingPos);
                    new Thread(PendingCancelTimeoutThread).Start(routPos.Symbol);
                }
                CancelPositionWrapper cancelWrapper = new CancelPositionWrapper(routPos, Config);
                return OrderRouter.ProcessMessage(cancelWrapper);
            }
            else
                return CMState.BuildSuccess();
        
        }
        
        protected CMState RunClose(Position openRoutingPos, MonitoringPosition monfPos, PortfolioPosition portfPos)
        {
            if (openRoutingPos.PosStatus == PositionStatus.Filled)
            {
                if (Security.GetSecurityType(Config.SecurityTypes) == SecurityType.FUT)
                    LoadCloseFuturePos(openRoutingPos, monfPos, portfPos);
                else
                    LoadCloseRegularPos(openRoutingPos, monfPos, portfPos);
                monfPos.Closing = true;
                PositionWrapper posWrapper = new PositionWrapper(portfPos.ClosingPosition, Config);
                return OrderRouter.ProcessMessage(posWrapper);
            }
            else if (openRoutingPos.PositionRouting())
            {
                DoLog(string.Format("Cancelling routing pos for symbol {0} before closing (status={1} posId={2})",
                        openRoutingPos.Security.Symbol,openRoutingPos.PosStatus,openRoutingPos.PosId),Constants.MessageType.Information);
                return CancelRoutingPos(openRoutingPos, portfPos);
            }
            else
            {
                DoLog(string.Format("{0} Aborting  position on invalid state. Symbol {1} Qty={2} PosStatus={3} PosId={4}", 
                        portfPos.TradeDirection, portfPos.OpeningPosition.Security.Symbol, portfPos.Qty,
                        openRoutingPos.PosStatus.ToString(),openRoutingPos.PosId), 
                    Constants.MessageType.Information);
                return CMState.BuildSuccess();
            }
        }
        
        protected void ProcessOrderRejection(PortfolioPosition tradPos, ExecutionReport report)
        {
            if (tradPos.CurrentPos().PosStatus == PositionStatus.PendingNew)
            {
                //A position was rejected, we remove it and Log what happened\
                EvalRemoval(tradPos, report);
                DoLog(string.Format("@{0} WARNING - Opening on position rejected for symbol {1} (PosId {3}):{2} ", 
                    Config.Name, tradPos.OpeningPosition.Security.Symbol, report.Text,
                    tradPos.CurrentPos().PosId), Constants.MessageType.Information);

            }
            else if (tradPos.CurrentPos().PositionRouting()) //OPEN:most probably an update failed--> we do nothing
            {
                EvalRemoval(tradPos, report);

                DoLog(string.Format("@{0} WARNING-Action on OPEN position rejected for symbol {1} (PosId={3}):{2} ",
                    Config.Name, tradPos.OpeningPosition.Security.Symbol, report.Text,
                    tradPos.CurrentPos().PosId), Constants.MessageType.Error);
            }
            else if (tradPos.CurrentPos().PositionNoLongerActive())//CLOSED most probably an update failed--> we do nothing
            {
                //The action that created the rejection state (Ex: the order was canceled or filled) should be
                //handled through the proper execution report
                DoLog(string.Format("@{0} WARNING-Action on CLOSED position rejected for symbol {1} (PosId={3}):{2} ", 
                    Config.Name, tradPos.OpeningPosition.Security.Symbol, report.Text,
                    tradPos.CurrentPos().PosId), Constants.MessageType.Error);
            }
        
        }
        
        protected void ProcessOrderCancellation(PortfolioPosition tradPos,ExecutionReport report)
        {
            lock (PendingCancels)
            {
                //We canceled a position that has to be closed!
                PortfolioPosition pendImbPos = PendingCancels[report.Order.Symbol];
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
                           
                            PortfolioPositions.TryRemove(tradPos.OpeningPosition.Security.Symbol,out _);
                            DoLog(string.Format("ER Cancelled for Pending Cancel for symbol {0} (PosId={1}): We were opening the position position. Live Qty<flat>=0",
                                                      report.Order.Symbol,tradPos.OpeningPosition.PosId), Main.Common.Util.Constants.MessageType.Information);

                            //It was not executed. We can remove the Position
                        }
                    }

                    DoLog(string.Format("Cancel Closing position for symbol {0} - The portfolio position is still OPEN", report.Order.Symbol),Main.Common.Util.Constants.MessageType.Information);
                    MonitorPositions[tradPos.OpeningPosition.Security.Symbol].Closing = false;
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
        
        protected void LogExecutionReport(PortfolioPosition tradingPos, ExecutionReport report)
        {

            DoLog(string.Format("{0} Position {7} ER on Position. Symbol {1} ExecType={7} OrdStatus={8} Qty={2} CumQty={3} LeavesQty={4} AvgPx={5} First Leg={6}",
                tradingPos.TradeDirection, tradingPos.OpeningPosition.Security.Symbol, tradingPos.Qty, tradingPos.CurrentPos().CumQty, tradingPos.CurrentPos().LeavesQty,
                tradingPos.CurrentPos().AvgPx, tradingPos.IsFirstLeg(), report.ExecType,report.OrdStatus), Constants.MessageType.Information);
        }


        protected abstract void LoadMonitorsAndRequestMarketData();

        //protected virtual void LoadMonitorsAndRequestMarketData()
        //{
        //    Thread.Sleep(5000);
        //    foreach (string symbol in Config.StocksToMonitor)
        //    {
        //        if (!MonitorPositions.ContainsKey(symbol))
        //        {
        //            Security sec = new Security()
        //            {
        //                Symbol = symbol,
        //                SecType = Security.GetSecurityType(Config.SecurityTypes),
        //                MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
        //                Currency = Config.Currency,
        //                Exchange = Config.Exchange
        //            };

        //            MonitoringPosition portfPos = new MonitoringPosition()
        //            {
        //                Security = sec,
        //                DecimalRounding = Config.DecimalRounding,
        //            };

        //            //1- We add the current security to monitor
        //            MonitorPositions.Add(symbol, portfPos);

        //            Securities.Add(sec);//So far, this is all wehave regarding the Securities

        //            //2- We request market data

        //            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
        //            MarketDataRequestCounter++;
        //            OnMessageRcv(wrapper);
        //        }
        //    }
        //}

        protected void DoPersistThread(object param)
        {
            try
            {
                PortfolioPosition trdPos = (PortfolioPosition) param;
                DoPersist(trdPos);
            }
            catch (Exception e)
            {
                DoLog(String.Format("Critical error persisting trade:{0}-{1}",e.Message,e.StackTrace),Constants.MessageType.Error);
            }
           
        }

        protected void UpdateLastPrice(MonitoringPosition portfPos,MarketData md)
        {
            if (PortfolioPositions.ContainsKey(portfPos.Security.Symbol))
            {
                PortfolioPosition trdPos = PortfolioPositions[portfPos.Security.Symbol];

                if(trdPos.OpeningPosition!=null && trdPos.OpeningPosition.CumQty>0)
                {
                    trdPos.LastPrice = md.Trade;
                    Thread persitThread = new Thread(new ParameterizedThreadStart(DoPersistThread));
                    persitThread.Start(trdPos);
                }

            }
        
        }
        
        protected bool IsTradingTime()
        {
            bool validOpening = false;
            if (!string.IsNullOrEmpty(Config.OpeningTime))
            {
                validOpening= DateTimeManager.Now > MarketTimer.GetTodayDateTime(Config.OpeningTime);
            }
            else
                validOpening = true;

            bool validClosing = false;

            if (ClosingTimeManager.ClosingTime.HasValue)
            {
                validClosing = DateTimeManager.Now < ClosingTimeManager.ClosingTime.Value;
            }
            else if (!string.IsNullOrEmpty(Config.ClosingTime))
            {
                validClosing = DateTimeManager.Now < MarketTimer.GetTodayDateTime(Config.ClosingTime);
            }
            else
                validClosing = true;

            return validOpening && validClosing;


            
        }
        
        private void CloseOnTradingTimeOff()
        {
            try
            {
                while (true)
                {
                    bool foundToClose = false;
                   
                    if (!IsTradingTime())
                    {
                        foreach (MonitoringPosition portfPos in MonitorPositions.Values)
                        {
                            if (PortfolioPositions.ContainsKey(portfPos.Security.Symbol))
                            {
                                PortfolioPosition tradPos = PortfolioPositions[portfPos.Security.Symbol];
                                if (tradPos.OpeningPosition != null)
                                {
                                    foundToClose = true;
                                    RunClose(tradPos.OpeningPosition, portfPos, tradPos);
                                    DoLog(
                                        string.Format(
                                            "{0} Position Closed on trading closed @{3}. Symbol {1} Qty={2} ",
                                            tradPos.TradeDirection, tradPos.OpeningPosition.Security.Symbol,
                                            tradPos.Qty,Config.ClosingTime), Constants.MessageType.Information);
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
        
        protected  async void  ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                SecurityList secList = SecurityListConverter.GetSecurityList(wrapper, Config);
                Securities = secList.Securities;
                LoadTradingParameters();
                DoLog(string.Format("@{0} Saving security list:{1} securities ", Config.Name, secList.Securities != null ? secList.Securities.Count : 0), Constants.MessageType.Information);
                
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0} Error processing security list:{1} ", Config.Name, ex.Message), Constants.MessageType.Error);
            }
        
        }
        
        #endregion
        
        #region Public Methods
        
        protected virtual CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog($"Incoming message from order routing w/ Action {wrapper.GetAction()}: " + wrapper.ToString(), Constants.MessageType.Information);

                if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                {
                    Thread ProcessExecutionReportThread = new Thread(new ParameterizedThreadStart(ProcessExecutionReport));
                    ProcessExecutionReportThread.Start(wrapper);
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST_REQUEST)
                {
                    OnMessageRcv(wrapper);
                }
               
                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        protected void CloseTradingDay()
        {
            if (TradingStarted && EndTime == null)
            {
                EndTime = DateTimeManager.Now;
                TradingBacktestingManager.EndTradingDay();
            }
        }

        public virtual CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    //SDoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    ProcessMarketData(wrapper);
                    return CMState.BuildSuccess();
                }

                if (wrapper.GetAction() == Actions.HISTORICAL_PRICES)
                {
                    ProcessHistoricalPrices(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    ProcessSecurityList(wrapper);
                    return OrderRouter.ProcessMessage(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Config.Name)));
            }
            catch (Exception ex)
            {
                DoLog( $"ERROR @ProcessMessage { Config.Name } : {ex.Message}", Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected ICommunicationModule LoadModules(string module,string configFile, OnLogMessage pOnLogMsg)
        {
            DoLog($"Initializing {module} " , Constants.MessageType.Information);
            if (!string.IsNullOrEmpty(module))
            {
                var typeModule = Type.GetType(module);
                if (typeModule != null)
                {
                    ICommunicationModule dest = (ICommunicationModule)Activator.CreateInstance(typeModule);
                    dest.Initialize(ProcessOutgoing, pOnLogMsg, configFile);
                    return dest;
                }
                else
                    throw new Exception("assembly not found: " + Config.OrderRouter);
            }
            else
            {
                DoLog($"{module} not found. It will not be initialized", Constants.MessageType.Error);
                return null;
            }

        }

        
        
        public virtual bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                StartTime = DateTimeManager.Now;
                LastCounterResetTime = StartTime;

                tLock = new object();
                tSynchronizationLock = new object();
                tPersistLock =new object();
                MonitorPositions = new Dictionary<string, MonitoringPosition>();
                PortfolioPositions = new ConcurrentDictionary<string, PortfolioPosition>();
                PendingCancels = new Dictionary<string, PortfolioPosition>();
                MarketDataConverter = new MarketDataConverter();
                ExecutionReportConverter = new ExecutionReportConverter();
                SecurityListConverter = new SecurityListConverter();
                Securities = new List<Security>();

                NextPosId = 1;
                PositionIdTranslator = new PositionIdTranslator(NextPosId);

                if(Config!=null && Config.OrderRouter!=null)
                    OrderRouter = LoadModules(Config.OrderRouter, Config.OrderRouterConfigFile, pOnLogMsg);

                LoadMonitorsAndRequestMarketData();

                Thread historicalPricesThread = new Thread(new ParameterizedThreadStart(DoRequestHistoricalPricesThread));
                historicalPricesThread.Start();

                Thread secListReqThread = new Thread(new ParameterizedThreadStart(DoRequestSecurityListThread));
                secListReqThread.Start();

                ResetEveryNMinutesThread = new Thread(ResetEveryNMinutes);
                ResetEveryNMinutesThread.Start();
                
                //CloseOnTradingTimeOffThread = new Thread(CloseOnTradingTimeOff);
                //CloseOnTradingTimeOffThread.Start();

                LoadPreviousTradingPositions();
                
                RequestOrderStatuses();
                

                return true;
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
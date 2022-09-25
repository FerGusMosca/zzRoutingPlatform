using System;
using System.Collections.Generic;
using System.Threading;
using DayCurrenciesTrading.BusinessEntities;
using DayCurrenciesTrading.Common.Configuration;
using DayCurrenciesTrading.Common.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;
using zHFT.StrategyHandlers.Common.Converters;

namespace DayCurrenciesTrading
{
    public class DayCurrenciesTrading: DayCurrencyBase,  ICommunicationModule, ILogger
    {
        
        #region Public Consts

        protected static string _PAIR_SEPARATOR_ORIG = "$";
        protected static string _PAIR_SEPARATOR_DEST = "";
        
        #endregion
        
        #region Public Attributes
        
        protected MarketDataConverter MarketDataConverter { get; set; }
        
        protected ExecutionReportConverter ExecutionReportConverter { get; set; }
        
        protected Configuration Configuration
        {
            get { return (Configuration)Config; }
            set { Config = value; }
        }
        
        public Dictionary<int, Security> MDReqs  { get; set; }
        
        public Dictionary<string, CurrencyPairMonitoringPosition> CurrencyPairMonitoringPositions { get; set; }
        
        #endregion
        
        #region Protected Methods

        protected void InitiateMonitoringPosition(Security pair)
        {
            CurrencyPairMonitoringPosition pos = new CurrencyPairMonitoringPosition(pair, Configuration);

            if (!CurrencyPairMonitoringPositions.ContainsKey(pair.Symbol))
            {
                CurrencyPairMonitoringPositions.Add(pair.Symbol, pos);
            }
            else
            {
                throw new Exception(string.Format(
                    "CRITICAL error creating monitoring position: The position for symbol {0} is already added ",
                    pair.Symbol));
            }
        }

        protected void MarketDataRequestThread()
        {

            try
            {
                int mdReqId = GetNextMDReqId();

                foreach (string pair in Configuration.PairToMonitor)
                {
                    DoLog(string.Format("Requesting Market Data for Security {0}",pair),Constants.MessageType.Information);

                    lock (CurrencyPairMonitoringPositions)
                    {

                        Security pairSec = SecurityConverter.GetSecurityFullSymbol(pair);

                        InitiateMonitoringPosition(pairSec);
                        MDReqs.Add(mdReqId, pairSec);
                        MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(mdReqId, pairSec,SubscriptionRequestType.SnapshotAndUpdates);
                        CMState reqState = MarketClientModule.ProcessMessage(wrapper);

                        if (!reqState.Success)
                            throw reqState.Exception;

                        mdReqId++;
                    }
                }

            }
            catch (Exception e)
            {
                DoLog(string.Format("{0}-ERROR Running MarketDataRequestThread:{1}", Configuration.Name, e.Message),
                    Constants.MessageType.Error);
            }
        }
        
        private void EvalClosingPosition(CurrencyPairMonitoringPosition monPos)
        {
            if (monPos.IsOpenPosition())
            {
                if (monPos.CloseLongPosition() || monPos.CloseShortPosition())
                {
                    
                    DoLog(string.Format("Closing {0} Position on market. Symbol {1} Qty={2} PosId={3} ",
                            monPos.TradeDirection(), monPos.Pair.Symbol, monPos.LastRoutingOpeningPosition.Qty,
                            monPos.LastRoutingClosingPosition != null ? monPos.LastRoutingClosingPosition.PosId : "-"),
                        Constants.MessageType.Information);
                
                    Position routingClosPos =  RunClose(monPos.LastRoutingOpeningPosition);

                    monPos.LastRoutingClosingPosition = routingClosPos;
                }
            }
        }
        
        protected virtual Position LoadNewRegularPos(Security sec, Side side)
        {

            Position pos = new Position()
            {

                Security = sec,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Configuration.PositionSizeInCash,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = ""
                //StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct),
                
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            return pos;
        }
        protected Position LoadNewPos(CurrencyPairMonitoringPosition monPos, Side side)
        {
            Position routingPos= LoadNewRegularPos(monPos.Pair, side);
            
            PositionWrapper posWrapper = new PositionWrapper(routingPos, Config);
            monPos.LastRoutingOpeningPosition = routingPos;
            monPos.LastRoutingClosingPosition = null;
                    
            CMState state = OrderRouter.ProcessMessage(posWrapper);

            if (!state.Success)
                throw state.Exception;

            DoLog(string.Format("{0} Position Opened to market. Symbol {1} CashQty={2}  PosId={3}", monPos.TradeDirection(),monPos.Pair.Symbol, routingPos.CashQty, routingPos.PosId), Constants.MessageType.Information);

            return routingPos;
        }

        protected void EvalOpeningClosingPositions(CurrencyPairMonitoringPosition monPos)
        {
            if (monPos.CanOpenRoutingPosition())
            {
                //Ready to open a new routing position
                if (monPos.LongSignalTriggered())
                {
                    LoadNewPos(monPos, Side.Buy);

                }
                else if(monPos.ShortSignalTriggered())
                { 
                    LoadNewPos(monPos, Side.Sell);
                }
            }
            else
            {
                EvalClosingPosition(monPos);
            }
        }

        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            lock (CurrencyPairMonitoringPositions)
            {
                if (CurrencyPairMonitoringPositions.ContainsKey(md.Security.Symbol))
                {
                    DoLog(string.Format("{0}- Received Market Data For Pair {1}:{2}", Configuration.Name, md.Security.Symbol,md.ToString()), 
                        Constants.MessageType.Information);

                    CurrencyPairMonitoringPosition monPos = CurrencyPairMonitoringPositions[md.Security.Symbol];
                    
                    monPos.UpdatePrice(md);
                    
                    EvalOpeningClosingPositions(monPos);
                   //UpdateLastPrice(secImb, md);
                }
            }
            
            OrderRouter.ProcessMessage(wrapper);

            return CMState.BuildSuccess();
        }

        #endregion
        
        #region Execution Reports
        
        protected void ProcessOrderCancellation(CurrencyPairMonitoringPosition monPos,ExecutionReport report)
        {
            lock (PendingCancels)
            {
                //We canceled a position that has to be closed!
                Position cxlPos = PendingCancels[report.Order.Symbol];
                DoLog(string.Format("Recv ER for Pending Cancel position for symbol {0}", report.Order.Symbol), Constants.MessageType.Information);

                if (report.ExecType == ExecType.Canceled)
                {

                    if (monPos.LastRoutingClosingPosition != null)//I cancelled a Closing Position
                    {

                        //This is what is net open from the position
                        monPos.LastRoutingOpeningPosition.CumQty -= monPos.LastRoutingOpeningPosition.CumQty;
                        DoLog(string.Format("ER Cancelled for Pending Cancel for symbol {0} (posId={2}): We were closing the closing position. New Live Qty={1}", 
                                                   report.Order.Symbol, monPos.LastRoutingOpeningPosition.CumQty,monPos.LastRoutingOpeningPosition.PosId), 
                                                    Constants.MessageType.Information);

                        monPos.LastRoutingClosingPosition = null;
                        
                    }
                    else 
                    {
                        if (monPos.LastRoutingOpeningPosition.CumQty > 0) //I cancelled an opening position --> I will have Qty=CumQty --> It will be processed in the next MARKET_DATA event
                        {
                            monPos.LastRoutingOpeningPosition.PosStatus = PositionStatus.Filled;
                            DoLog(string.Format("ER Cancelled for Pending Cancel for symbol {0} (PosId={2}): We were opening the position position. Live Qty={1}", 
                                                        report.Order.Symbol, monPos.LastRoutingOpeningPosition.CumQty,monPos.LastRoutingOpeningPosition.PosId),Constants.MessageType.Information);

                        }
                        else
                        {
                           
                            DoLog(string.Format("ER Cancelled for Pending Cancel for symbol {0} (PosId={1}): We were opening the position position. Live Qty<flat>=0",
                                                      report.Order.Symbol,monPos.LastRoutingOpeningPosition.PosId), Constants.MessageType.Information);

                            //It was not executed. We can remove the ImbalancePosition
                        }
                    }

                    //Now we can finally close the position
                    PendingCancels.Remove(report.Order.Symbol);
                }
                else//something happened with the cancelation -> we have to try again and log
                {
                    //as the positions stays as Partially Filled we will try again, on and on
                    PendingCancels.Remove(report.Order.Symbol);
                    DoLog(string.Format("ERROR-Problems with cancellation for Pending Cancel for symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}", 
                                        report.Order.Symbol,report.OrdStatus,report.ExecType,report.Text,monPos.CurrentPos().PosId), 
                                        Constants.MessageType.Error);

                }
            }
        }
        
        protected void EvalRemoval(CurrencyPairMonitoringPosition monPos, ExecutionReport report)
        {
            monPos.CurrentPos().PositionCanceledOrRejected = true;
            monPos.CurrentPos().PositionCleared = false;
            monPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
        }
        
        protected void ProcessOrderRejection(CurrencyPairMonitoringPosition monPos, ExecutionReport report)
        {
            if (monPos.CurrentPos().PosStatus == PositionStatus.PendingNew)
            {
                //A position was rejected, we remove it and Log what happened\
                EvalRemoval(monPos, report);
                DoLog(string.Format("@{0} WARNING - Opening on position rejected for symbol {1} (PosId {3}):{2} ", 
                    Configuration.Name, monPos.LastRoutingOpeningPosition.Security.Symbol, report.Text,
                    monPos.CurrentPos().PosId), Constants.MessageType.Information);

            }
            else if (monPos.CurrentPos().PositionRouting()) //OPEN:most probably an update failed--> we do nothing
            {
                DoLog(string.Format("@{0} WARNING-Action on OPEN position rejected for symbol {1} (PosId={3}):{2} ",
                    Configuration.Name, monPos.LastRoutingOpeningPosition.Security.Symbol, report.Text,
                    monPos.CurrentPos().PosId), Constants.MessageType.Error);
            }
            else if (monPos.CurrentPos().PositionNoLongerActive())//CLOSED most probably an update failed--> we do nothing
            {
                //The action that created the rejection state (Ex: the order was canceled or filled) should be
                //handled through the proper execution report
                DoLog(string.Format("@{0} WARNING-Action on CLOSED position rejected for symbol {1} (PosId={3}):{2} ", 
                    Configuration.Name, monPos.LastRoutingOpeningPosition.Security.Symbol, report.Text,
                    monPos.CurrentPos().PosId), Constants.MessageType.Error);
            }
        
        }
        
         protected void AssignMainERParameters(CurrencyPairMonitoringPosition monPos,ExecutionReport report)
        {
            if (!report.IsCancelationExecutionReport())
            {
                monPos.CurrentPos().CumQty = report.CumQty;
                monPos.CurrentPos().LeavesQty = report.LeavesQty;
                monPos.CurrentPos().AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                monPos.CurrentPos().SetPositionStatusFromExecutionStatus(report.OrdStatus);
                monPos.CurrentPos().ExecutionReports.Add(report);

                if (report.OrdStatus == OrdStatus.Filled)
                {
                    //SecurityImbalanceManager.PersistSecurityImbalanceTrade(imbPos);//first leg and second leg
                    //TODO Implement persistance
                    
                    if (!monPos.IsFirstLeg())
                    {
                        DoLog(string.Format("DB-Closing position for symbol {0} (CumQty={1})",monPos.CurrentPos().Security.Symbol,report.CumQty),Constants.MessageType.Information);
                        //monPos.Closing = false;
                    }
                    else
                    {
                        DoLog(string.Format("DB-Fully opened  {2} position for symbol {0} (CumQty={1})",
                                                   monPos.Pair.Symbol,report.CumQty,
                                                   monPos.TradeDirection()),Constants.MessageType.Information);
                    }
                }
            }
            else
            {
                if (PendingCancels.ContainsKey(report.Order.Symbol))
                {
                    ProcessOrderCancellation(monPos, report);
                }
                else
                {
                    if (report.OrdStatus == OrdStatus.Rejected)
                    {
                        DoLog(string.Format("Rejected execution report symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}",
                                                report.Order.Symbol, report.OrdStatus, report.ExecType, report.Text,monPos.CurrentPos().PosId),
                                                Constants.MessageType.Information);
                        ProcessOrderRejection(monPos, report);
                    }
                    else
                    {
                        DoLog(string.Format("WARNING-Recv ER for symbol {0} (PosId={4}): ER Status={1} ER ExecType={1} ER Text={3}",
                                                report.Order.Symbol, report.OrdStatus, report.ExecType, report.Text,monPos.CurrentPos().PosId),
                                                Constants.MessageType.Information);
                        EvalRemoval(monPos, report);
                    }
                }
            }
        }
         
         protected void LogExecutionReport(CurrencyPairMonitoringPosition monPos, ExecutionReport report)
         {

             DoLog(string.Format("{0} Position {7} ER on Position. Symbol {1} ExecType={7} OrdStatus={8} Qty={2} CymQty={3} LeavesQty={4} AvgPx={5} First Leg={6}",
                 monPos.TradeDirection(), monPos.LastRoutingOpeningPosition.Security.Symbol, monPos.CurrentPos().Qty, monPos.CurrentPos().CumQty, monPos.CurrentPos().LeavesQty,
                 monPos.CurrentPos().AvgPx, monPos.IsFirstLeg(), report.ExecType,report.OrdStatus), Constants.MessageType.Information);

         }
        
        protected void ProcessExecutionReport(object param)
        { 
            Wrapper wrapper = (Wrapper)param;

            try
            {
                lock (CurrencyPairMonitoringPositions)
                {
                    ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                    //EvalCancellingOrdersOnStartup(report);
                     
                    if (CurrencyPairMonitoringPositions.ContainsKey(report.Order.Symbol))
                    {
                        CurrencyPairMonitoringPosition monPos = CurrencyPairMonitoringPositions[report.Order.Symbol];
                        AssignMainERParameters(monPos, report);
                        LogExecutionReport(monPos, report);
                    }
                }
             
            }
            catch (Exception e)
            {
                DoLog(string.Format("Error processing execution report {0}:{1}-{2}",
                    wrapper != null ? wrapper.ToString() : "?", e.Message,e.StackTrace), Constants.MessageType.Information);
            }
        }
        
        
        #endregion
        
        #region DayTradingStrategyBase Methods
        
        public CMState ProcessIncoming(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog("Processing Market Data:" + wrapper.ToString(), Constants.MessageType.Information);

                    return ProcessMarketData(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Configuration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + Configuration.Name + ":" + ex.Message, Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }
        
        public CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog("Incoming message from order routing: " + wrapper.ToString(), Constants.MessageType.Information);

                if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                {
                    Thread ProcessExecutionReportThread = new Thread(ProcessExecutionReport);
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
        
        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Configuration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + Configuration.Name + ":" + ex.Message, Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;
            
            if (ConfigLoader.LoadConfig(this, configFile))
            {
                
                MDReqs=new Dictionary<int, Security>();

                NextPosId = 1;
                
                MarketDataConverter=new MarketDataConverter();
                ExecutionReportConverter = new ExecutionReportConverter();

                CurrencyPairMonitoringPositions = new Dictionary<string, CurrencyPairMonitoringPosition>();
                PendingCancels=new Dictionary<string, Position>();

                MarketClientModule = LoadModule(Configuration.IncomingModule, "Market Data Client Module");
                MarketClientModule.Initialize(ProcessIncoming, pOnLogMsg, Configuration.IncomingConfigPath);
                
                OrderRouter = LoadModule(Configuration.OutgoingModule, "Order Router Module");
                OrderRouter.Initialize(ProcessOutgoing, pOnLogMsg, Configuration.OutgoingConfigPath);

                Thread marketDataReqThread = new Thread(MarketDataRequestThread);
                marketDataReqThread.Start();
                
                DoLog(string.Format("DayCurrencies successfully initialized..."), Constants.MessageType.Information);
                
                return true;

            }
            else
            {
                DoLog(string.Format("ERROR Initializing DayCurrencies config file..."), Constants.MessageType.Error);
                return false;
            }
        }

        

        #endregion
    }
}
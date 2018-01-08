using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.Main.DataAccessLayer.Managers;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;

namespace zHFT.StrategyHandler.LogicLayer
{
    public abstract class StrategyBase : ICommunicationModule
    {
        #region Protected Attributes

        protected ICommunicationModule OrderRouter { get; set; }

        public string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected IConfiguration Config { get; set; }

        protected object tLock { get; set; }

        protected Dictionary<string, ExecutionSummary> ExecutionSummaries { get; set; }

        protected Dictionary<string, Position> Positions { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected PositionConverter PositionConverter { get; set; }

        protected int NextPosId { get; set; }

        protected ExecutionSummaryManager ExecutionSummaryManager { get; set; }

        protected Common.Configuration.StrategyConfiguration StrategyConfiguration
        {
            get { return (Common.Configuration.StrategyConfiguration)Config; }
            set { Config = value; }
        }

        #endregion

        #region Abstract Methods

        protected abstract void DoLoadConfig(string configFile, List<string> noValueFields);

        protected abstract bool OnInitialize();

        protected abstract CMState ProcessMarketData(Wrapper wrapper);

        protected abstract void OnEvalExecutionSummary(object param);

        protected abstract void OnProcessNewPositionCanceled(Position pos);

        protected abstract void UnsuscribeMarketData(Position pos);
       
        #endregion

        #region Protected Methods

        //I won't do anything on a canceled or Rejected Position
        protected bool EvalPositionCleared(string symbol)
        {

            if (!Positions.ContainsKey(symbol))
            {
                return false;
            }
            else if (Positions.ContainsKey(symbol))
            {
                Position pos = Positions[symbol];

                return pos.PositionCleared;
            }
            else
                return false;

        }

        //I won't do anything on a canceled or Rejected Position
        protected bool EvalPositionCanceledOrRejected(string symbol)
        {

            if (!Positions.ContainsKey(symbol))
            {
                return false;
            }
            else if (Positions.ContainsKey(symbol))
            {
                Position pos = Positions[symbol];

                return pos.PositionCanceledOrRejected;
            }
            else
                return false;

        }

        //I wont't open twice the same position
        protected bool EvalPositionOpened(string symbol)
        {
            if (!Positions.ContainsKey(symbol))
            {
                return false;
            }
            else if (Positions.ContainsKey(symbol))
            {
                Position pos = Positions[symbol];
                return !pos.NewPosition;//NewPosition=false ---> The position was opened - NewPosition=true ---> The position was never opened
            }
            else
                return true;

        }

        private void EvalExecutionSummary(ExecutionSummary summary)
        {
            //if (summary.AvgPx == null)
            //    throw new Exception(string.Format("@{0}: Could not process a null average price for symbol {1}", StrategyConfiguration.Name,summary.Symbol));

            //We just handle one stock in this strategy, so there won't be more execution summaries
            if (summary != null)
            {
                Thread OnEvalExecutionSummaryThread = new Thread(new ParameterizedThreadStart(OnEvalExecutionSummary));
                OnEvalExecutionSummaryThread.Start(summary);
            }
            else
                throw new Exception(string.Format("@{0}:Could not find Execution Summary for unknown symbol. Cancelling all orders!", StrategyConfiguration.Name));
        }

        protected  void CancelAllNotCleared()
        {
            try
            {
                foreach (ExecutionSummary sum in ExecutionSummaries.Values)
                {
                    if (!sum.Position.PositionCleared && !sum.Position.PositionCanceledOrRejected)
                    {
                        DoLog(string.Format("@{0}: Cancelling position on symbol {1} ", StrategyConfiguration.Name, sum.Symbol), Constants.MessageType.Information);
                        CancelPositionWrapper cancelWrapper = new CancelPositionWrapper(sum.Position, Config);
                        if (OrderRouter.ProcessMessage(cancelWrapper).Success)
                        {
                            sum.Position.PosStatus = PositionStatus.Canceled;
                            sum.Position.PositionCanceledOrRejected = true;
                            sum.Text = "Position canceled on massive depuration";
                            SaveExecutionSummary(sum);
                            UnsuscribeMarketData(sum.Position);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@CancelAllNotCleared: Critical error cancelling all positions: {0}",  ex.Message), Constants.MessageType.Error);
            }
        }

        protected void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        protected bool LoadConfig(string configFile)
        {
            DoLog(DateTime.Now.ToString() + "MomentumStrategyBase.LoadConfig", Constants.MessageType.Information);

            DoLog("Loading config:" + configFile, Constants.MessageType.Information);
            if (!File.Exists(configFile))
            {
                DoLog(configFile + " does not exists", Constants.MessageType.Error);
                return false;
            }

            List<string> noValueFields = new List<string>();
            DoLog("Processing config:" + configFile, Constants.MessageType.Information);
            try
            {
                DoLoadConfig(configFile, noValueFields);
                DoLog("Ending GetConfiguracion " + configFile, Constants.MessageType.Information);
            }
            catch (Exception e)
            {
                DoLog("Error recovering config " + configFile + ": " + e.Message, Constants.MessageType.Error);
                return false;
            }

            if (noValueFields.Count > 0)
                noValueFields.ForEach(s => DoLog(string.Format(Constants.FieldMissing, s), Constants.MessageType.Error));

            return true;
        }

        protected void ProcessNewPositionCanceled(Wrapper wrapper)
        {
            lock (tLock)
            {
                try
                {
                    Position pos = PositionConverter.GetPosition(wrapper, Config);
                    if (pos != null)
                    {

                        if (Positions.ContainsKey(pos.Symbol))
                            Positions.Remove(pos.Symbol);
                        else
                            throw new Exception(string.Format("Could not find canceled position for symbol {0}", pos.Symbol));

                        if (ExecutionSummaries.ContainsKey(pos.Symbol))
                            ExecutionSummaries.Remove(pos.Symbol);
                    }
                    else
                        throw new Exception("Could not find symbol for new position canceled");
                }
                catch (Exception ex)
                {
                    DoLog("Critical error processing new position canceled! Canceling all orders. Error:" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                    CancelAllNotCleared();
                }
            }
        
        }

        private void UpdateExecutionSummaryData(ExecutionSummary summary, ExecutionReport report)
        {
            summary.Position.ExecutionReports.Add(report);

            if (!summary.Position.Orders.Any(x => x.OrderId == report.Order.OrderId))
            {
                summary.Position.Orders.Add(report.Order);
            }

        
        }

        protected void ProcessExecutionReport(object param)
        {
            Wrapper wrapper = (Wrapper)param;
            lock (tLock)
            {
                try
                {
                    ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                    if (report.Order!=null && report.Order.Symbol!=null && ExecutionSummaries.ContainsKey(report.Order.Symbol))
                    {
                        
                        ExecutionSummary summary = ExecutionSummaries[report.Order.Symbol];

                        if (summary == null)
                            throw new Exception(string.Format("Critical Error! Could not find summary for symbol {0}", (string)wrapper.GetField(ExecutionReportFields.Symbol)));


                        if (report != null)
                        {
                            if (report.ExecType == ExecType.Trade && report.OrdStatus == OrdStatus.PartiallyFilled)
                            {

                                DoLog(string.Format("Received partially filled for symbol {0}",summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);
                                summary.CumQty = report.CumQty;
                                summary.LeavesQty = report.LeavesQty;
                                summary.AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                                summary.Commission = report.Commission.HasValue ? (double?)report.Commission.Value : null;
                                summary.Position.SetPositionStatusFromExecution(report.ExecType);
                                UpdateExecutionSummaryData(summary, report);
                                EvalExecutionSummary(summary);
                                DoLog(string.Format("Partially filled for symbol {0} processed", summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);

                            }//Filled
                            else if (report.ExecType == ExecType.Trade && report.OrdStatus == OrdStatus.Filled)
                            {
                                DoLog(string.Format("Received filled for symbol {0}", summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);
                                summary.CumQty = report.CumQty;
                                summary.LeavesQty = report.LeavesQty;
                                summary.AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                                summary.Commission = report.Commission.HasValue ? (double?)report.Commission.Value : null;
                                summary.Position.PositionCleared = true;
                                summary.Position.SetPositionStatusFromExecution(report.ExecType);
                                summary.Position.PositionCanceledOrRejected = false;
                                UpdateExecutionSummaryData(summary, report);
                                EvalExecutionSummary(summary);
                                DoLog(string.Format("Filled for symbol {0} processed", summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);
                               
                            }
                            else if (report.ExecType == ExecType.DoneForDay || report.ExecType == ExecType.Stopped
                                     || report.ExecType == ExecType.Suspended || report.ExecType == ExecType.Rejected
                                     || report.ExecType == ExecType.Expired || report.ExecType == ExecType.Canceled)
                            {
                                //Position Canceled
                                DoLog(string.Format("Received {0} for symbol {1}",report.ExecType.ToString() ,summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);
                                summary.Position.PositionCanceledOrRejected = true;
                                summary.Position.PositionCleared = false;
                                summary.Position.SetPositionStatusFromExecution(report.ExecType);
                                summary.Text = report.Text;
                                UpdateExecutionSummaryData(summary, report);
                                EvalExecutionSummary(summary);
                                DoLog(string.Format("{0} for symbol {1} processed", report.ExecType.ToString(), summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);
                            }
                            else
                            {
                                DoLog(string.Format("Received {0} for symbol {1}", report.ExecType.ToString(), summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);
                                summary.CumQty = report.CumQty;
                                summary.LeavesQty = report.LeavesQty;
                                summary.AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                                summary.Commission = report.Commission.HasValue ? (double?)report.Commission.Value : null;
                                summary.Position.SetPositionStatusFromExecution(report.ExecType);
                                summary.Position.ExecutionReports.Add(report);
                                EvalExecutionSummary(summary);
                                DoLog(string.Format("{0} for symbol {1} processed", report.ExecType.ToString(), summary.Position.Symbol), Main.Common.Util.Constants.MessageType.Information);
                            }
                        }
                    }
                    else if(report.Order!=null && report.Order.Symbol!=null)
                        DoLog(string.Format("Received execution report for no longer processed symbol {0}",report.Order.Symbol) , Main.Common.Util.Constants.MessageType.Information);
                    else 
                        DoLog(string.Format("Received execution with no order"), Main.Common.Util.Constants.MessageType.Information);
                }
                catch (Exception ex)
                {
                    //Critical errors!
                    DoLog("Critical error processing execution report! Canceling all orders. Error:" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                }
            }

        }

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
                else if (wrapper.GetAction() == Actions.NEW_POSITION_CANCELED)
                {
                    ProcessNewPositionCanceled(wrapper);
                }


                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        protected CMState ProcessIncoming(Wrapper wrapper)
        {
            ////Not incoming module set
            return CMState.BuildFail(new Exception("No incoming module set!"));
        }

        private void SaveExecutionSummaryOnDB(ExecutionSummary summary)
        {
            if (ExecutionSummaryManager == null)
                throw new Exception("Exectuion summary manager not initialized!!");

            ExecutionSummaryManager.Insert(summary);
        }

        private void SaveExecutionSummaryOnExcel()
        {
            throw new Exception("Saving on Excel not implemented");
        }

        protected void SaveAndCleanAllPositions()
        {
            CancelAllNotCleared();
            SaveExecutionSummaries();
            ExecutionSummaries.Clear();
            Positions.Clear();
        }

        protected void SaveExecutionSummary(ExecutionSummary summary)
        {
            try
            {
                try
                {
                    if (StrategyConfiguration.ReportSavingBD())
                        SaveExecutionSummaryOnDB(summary);
                    else if (StrategyConfiguration.ReportSavingExcel())
                        SaveExecutionSummaryOnExcel();
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Critical error saving execution summary for symbol {0}: {1} - Inner: {2} ", summary.Position.Symbol, ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""), Constants.MessageType.Error);
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error saving execution summary {0} ", ex.Message), Constants.MessageType.Error);

            }
        
        }

        protected void SaveExecutionSummaries()
        {
            try
            {
                foreach (ExecutionSummary summary in ExecutionSummaries.Values)
                {
                    try
                    {
                        if (StrategyConfiguration.ReportSavingBD())
                            SaveExecutionSummaryOnDB(summary);
                        else if (StrategyConfiguration.ReportSavingExcel())
                            SaveExecutionSummaryOnExcel();
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("Critical error saving execution summary for symbol {0}: {1} - Inner: {2} ", summary.Position.Symbol, ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""), Constants.MessageType.Error);
                    }
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error saving execution summary {0} ",ex.Message), Constants.MessageType.Error);
            
            }
        }

        protected CMState OpenPositionOnMarket(ExecutionSummary summary)
        {
            if (summary != null && summary.Position != null)
            {
                //If processed failed, we won't try to open the position again so for the strategy handler
                //This is like an open position (NewPosition=false)
                PositionWrapper posWrapper = new PositionWrapper(summary.Position, Config);

                if (!ExecutionSummaries.ContainsKey(summary.Symbol))
                    ExecutionSummaries.Add(summary.Symbol, summary);

                if (!Positions.ContainsKey(summary.Position.Symbol))
                    Positions.Add(summary.Position.Security.Symbol, summary.Position);

                CMState state = OrderRouter.ProcessMessage(posWrapper);
                summary.Position.NewPosition = !state.Success;
                summary.Position.PosStatus = PositionStatus.PendingNew;
                return state;
            }
            else
                return CMState.BuildFail(new Exception("Could not open position on market because for a critical error!"));
        }

        #endregion

        #region Public Methods

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    DoLog("Initializing Order Router " + StrategyConfiguration.OrderRouter, Constants.MessageType.Information);
                    if (!string.IsNullOrEmpty(StrategyConfiguration.OrderRouter))
                    {
                        var typeOrderRouter = Type.GetType(StrategyConfiguration.OrderRouter);
                        if (typeOrderRouter != null)
                        {
                            OrderRouter = (ICommunicationModule)Activator.CreateInstance(typeOrderRouter);
                            OrderRouter.Initialize(ProcessOutgoing, pOnLogMsg, StrategyConfiguration.OrderRouterConfigFile);
                        }
                        else
                            throw new Exception("assembly not found: " + StrategyConfiguration.OrderRouter);
                    }
                    else
                        DoLog("Order Router not found. It will not be initialized", Constants.MessageType.Error);

                    NextPosId = 1;
                    Positions = new Dictionary<string, Position>();
                    ExecutionSummaries = new Dictionary<string, ExecutionSummary>();
                    ExecutionReportConverter = new ExecutionReportConverter();
                    MarketDataConverter = new MarketDataConverter();
                    PositionConverter = new PositionConverter();
                    tLock = new object();

                    if (StrategyConfiguration.ReportSavingBD())
                        ExecutionSummaryManager = new ExecutionSummaryManager(StrategyConfiguration.ReportSavingConnectionString);

                    //ExecutionSummaryManager.GetTest();
                    return OnInitialize();
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

        public CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    return ProcessMarketData(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), StrategyConfiguration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + StrategyConfiguration.Name + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion
    }
}

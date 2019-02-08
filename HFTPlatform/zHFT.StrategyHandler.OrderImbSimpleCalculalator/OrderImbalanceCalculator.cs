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
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderImbSimpleCalculator.BusinessEntities;
using zHFT.OrderImbSimpleCalculator.Common.Enums;
using zHFT.OrderImbSimpleCalculator.DataAccessLayer;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;


namespace zHFT.StrategyHandler.OrderImbSimpleCalculator
{
    public class OrderImbalanceCalculator : ICommunicationModule
    {
        #region Protected Attributes

        protected int NextPosId { get; set; }

        protected ICommunicationModule OrderRouter { get; set; }

        protected Dictionary<string, Position> Positions { get; set; }

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

        protected DateTime? LastPersistanceTime {get;set;}

        protected DateTime StartTime { get; set; }

        protected Thread SecImbalancePersistanceThread { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        #endregion

       

        #region Load Methods

        protected void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        protected  void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration().GetConfiguration<zHFT.OrderImbSimpleCalculator.Common.Configuration.Configuration>(configFile, noValueFields);
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

        #endregion

        #region Private Methods

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
                    SecType = SecurityType.CS,
                    Currency = Configuration.Currency,
                    Exchange = Configuration.Exchange
                };

                SecurityImbalance secImbalance = new SecurityImbalance() 
                {
                    Security = sec ,
                };

                //1- We add the current security to monitor
                SecurityImbalancesToMonitor.Add(symbol, secImbalance);

                //2- We request market data

                MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter,sec, SubscriptionRequestType.SnapshotAndUpdates);
                MarketDataRequestCounter++;
                OnMessageRcv(wrapper);
            }
        }

        private Position LoadNewPos(SecurityImbalance secImb, Side side)
        {

            Position pos = new Position()
            {
                Security = new Security()
                {
                    Symbol = secImb.Security.Symbol,
                    MarketData = null,
                    Currency = Configuration.Currency,
                    SecType = SecurityType.CS
                },
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = 5000d,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = null
            };

            pos.LoadPosId(NextPosId);
            NextPosId++;

            return pos;
        
        }

        private Position LoadClosePos(Position openPos)
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
                Side = openPos.Side==Side.Buy?Side.Sell:Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = openPos.CumQty,
                QuantityType = QuantityType.SHARES,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = null
            };


            pos.PositionCleared = true;
            pos.LoadPosId(NextPosId);
            NextPosId++;

            return pos;
        }

        private void EvalOpeningClosingPositions(SecurityImbalance secImb)
        {

            TimeSpan elapsed = DateTime.Now - StartTime;

            if (elapsed.TotalMinutes > 5)
            {
                if (secImb.AskSizeImbalance > 0.7m && !Positions.ContainsKey(secImb.Security.Symbol))
                {
                    Position pos = LoadNewPos(secImb, Side.Buy);
                    PositionWrapper posWrapper = new PositionWrapper(pos, Config);
                    Positions.Add(pos.Security.Symbol, pos);
                    CMState state = OrderRouter.ProcessMessage(posWrapper);
                
                
                }
                else if (secImb.BidSizeImbalance > 0.7m && !Positions.ContainsKey(secImb.Security.Symbol))
                {
                    Position pos = LoadNewPos(secImb, Side.Sell);
                    PositionWrapper posWrapper = new PositionWrapper(pos, Config);
                    Positions.Add(pos.Security.Symbol, pos);
                    CMState state = OrderRouter.ProcessMessage(posWrapper);
                
                }
                else if (secImb.BidSizeImbalance > 0.45m && secImb.BidSizeImbalance < 0.55m && Positions.ContainsKey(secImb.Security.Symbol))
                {
                    Position openPos = Positions[secImb.Security.Symbol];
                    if (openPos.PosStatus == PositionStatus.New || openPos.PosStatus == PositionStatus.Filled)
                    {
                        Position closePos = LoadClosePos(openPos);
                        PositionWrapper posWrapper = new PositionWrapper(closePos, Config);
                        Positions[secImb.Security.Symbol] = closePos;
                        CMState state = OrderRouter.ProcessMessage(posWrapper);
                    }
                }
                else if (secImb.AskSizeImbalance > 0.45m && secImb.AskSizeImbalance < 0.55m && Positions.ContainsKey(secImb.Security.Symbol))
                {
                    Position openPos = Positions[secImb.Security.Symbol];
                    if (openPos.PosStatus == PositionStatus.New || openPos.PosStatus == PositionStatus.Filled)
                    {
                        Position closePos = LoadClosePos(openPos);
                        PositionWrapper posWrapper = new PositionWrapper(closePos, Config);
                        Positions[secImb.Security.Symbol] = closePos;
                        CMState state = OrderRouter.ProcessMessage(posWrapper);
                    }
                }
            }
        }

        private CMState ProcessMarketData(Wrapper wrapper)
        {
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            lock (tLock)
            {
                if (SecurityImbalancesToMonitor.ContainsKey(md.Security.Symbol))
                {
                    SecurityImbalance secImb = SecurityImbalancesToMonitor[md.Security.Symbol];
                    secImb.Security.MarketData = md;
                    secImb.ProcessCounters();
                    EvalOpeningClosingPositions(secImb);
                }
            }

            OrderRouter.ProcessMessage(wrapper);

            return CMState.BuildSuccess();
        }

        protected void ProcessExecutionReport(object param)
        { 
             Wrapper wrapper = (Wrapper)param;
             lock (tLock)
             {
                 ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                 if (Positions.ContainsKey(report.Order.Symbol))
                 {
                     Position openPos = Positions[report.Order.Symbol];

                     if (report.ExecType == ExecType.Trade && report.OrdStatus == OrdStatus.PartiallyFilled)
                     {
                         openPos.CumQty = report.CumQty;
                         openPos.LeavesQty = report.LeavesQty;
                         openPos.AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                         openPos.SetPositionStatusFromExecution(report.ExecType);
                     }
                     else if (report.ExecType == ExecType.Trade && report.OrdStatus == OrdStatus.Filled)
                     {

                         openPos.CumQty = report.CumQty;
                         openPos.LeavesQty = report.LeavesQty;
                         openPos.AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                         openPos.SetPositionStatusFromExecution(report.ExecType);
                         openPos.PositionCanceledOrRejected = false;

                         if (openPos.PositionCleared)
                             Positions.Remove(openPos.Symbol);
                     }
                     else if (report.ExecType == ExecType.DoneForDay || report.ExecType == ExecType.Stopped
                                     || report.ExecType == ExecType.Suspended || report.ExecType == ExecType.Rejected
                                     || report.ExecType == ExecType.Expired || report.ExecType == ExecType.Canceled)
                     {

                         openPos.PositionCanceledOrRejected = true;
                         openPos.PositionCleared = false;
                         openPos.SetPositionStatusFromExecution(report.ExecType);

                     }
                     else
                     {
                         openPos.CumQty = report.CumQty;
                         openPos.LeavesQty = report.LeavesQty;
                         openPos.AvgPx = report.AvgPx.HasValue ? (double?)report.AvgPx.Value : null;
                         openPos.SetPositionStatusFromExecution(report.ExecType);
                         openPos.ExecutionReports.Add(report);
                     }
                 }
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
                else if (wrapper.GetAction() == Actions.NEW_POSITION_CANCELED)
                {
                    //ProcessNewPositionCanceled(wrapper);
                }


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

                if (LoadConfig(configFile))
                {
                    tLock = new object();
                    SecurityImbalanceManager = new SecurityImbalanceManager(Configuration.ConnectionString);
                    SecurityImbalancesToMonitor = new Dictionary<string, SecurityImbalance>();
                    Positions = new Dictionary<string, Position>();
                    MarketDataConverter = new MarketDataConverter();
                    ExecutionReportConverter = new ExecutionReportConverter();

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

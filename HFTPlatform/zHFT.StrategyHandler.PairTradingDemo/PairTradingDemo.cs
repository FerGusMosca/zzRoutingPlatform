using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Configuration;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.PairTradingDemo.Common.DTO;
using zHFT.StrategyHandler.PairTradingDemo.Service;
using zHFT.StrategyHandlers.Common.Converters;

namespace zHFT.StrategyHandler.PairTradingDemo
{
    public class PairTradingDemo : ICommunicationModule
    {
        #region Protected Attributes

        public string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected IConfiguration Config { get; set; }

        protected zHFT.StrategyHandler.PairTradingDemo.Common.Configuration.Configuration StrategyConfiguration
        {
            get { return (zHFT.StrategyHandler.PairTradingDemo.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected ICommunicationModule OrderRouter { get; set; }

        protected int NextPosId { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected PositionConverter PositionConverter { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        protected Dictionary<string, ExecutionSummary> ExecutionSummaries { get; set; }

        protected Dictionary<int, PairTradingRequest> PairTradingRequest { get; set; }

        protected object tLock { get; set; }

        protected HttpSelfHostServer server { get; set; }

        #endregion

        #region Private Static Consts

        private static int _POS_ID_SPREAD=10000;

        #endregion


        #region Protected Methods

        protected  void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
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

        protected void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        #endregion

        #region Private Methods

        private void InitializeRESTService()
        {
            try
            {
                DoLog(string.Format("Creating pair trading demo service for controller PairTradingDemoController on URL {0}",
                                    StrategyConfiguration.PairTradingRequestURL), Constants.MessageType.Information);

                HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(StrategyConfiguration.PairTradingRequestURL);

                config.Routes.MapHttpRoute(name: "DefaultApi",
                                           routeTemplate: "api/{controller}/{action}/{id}",
                                           defaults: new { id = RouteParameter.Optional });

                PairTradingDemoController.OnProcessPairTradingRequest += ProcessPairTradingRequest;
                PairTradingDemoController.OnProcessPairTradingStatus += ProcessPairTradingStatus;
                PairTradingDemoController.OnLog += DoLog;

                server = new HttpSelfHostServer(config);
                server.OpenAsync().Wait();
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error creating pair trading demo service for controller PairTradingDemoController on URL {0}:{1}",
                      StrategyConfiguration.PairTradingRequestURL, ex.Message), Constants.MessageType.Error);
            }
        
        }

        private void InitializeMergeTrading(PairTradingRequest rq)
        {

            Security secLong = new Security()
            {
                Symbol = rq.LongSymbol,
                Currency = StrategyConfiguration.Currency,
                Exchange = StrategyConfiguration.Exchange
            };

            Security secShort = new Security()
            {
                Symbol = rq.ShortSymbol,
                Currency = StrategyConfiguration.Currency,
                Exchange = StrategyConfiguration.Exchange
            };

            MarketDataRequestWrapper wrapperLong = new MarketDataRequestWrapper(secLong,SubscriptionRequestType.SnapshotAndUpdates);


            MarketDataRequestWrapper wrapperShort = new MarketDataRequestWrapper(secShort, SubscriptionRequestType.SnapshotAndUpdates);

            OnMessageRcv(wrapperLong);

            OnMessageRcv(wrapperShort);

            PairTradingRequest.Add(rq.Id, rq);

            rq.LastStatus = string.Format("Market Data Request Sent for symbols {0}-{1}",rq.LongSymbol,rq.ShortSymbol);

            rq.MarketDataRequestSent = true;

        }

        protected  CMState ProcessMarketData(Main.Common.Wrappers.Wrapper wrapper)
        {
            lock (tLock)
            {
                string symbol = (string)wrapper.GetField(MarketDataFields.Symbol);

                MarketData MarketData = MarketDataConverter.GetMarketData(wrapper, Config);

                IEnumerable<PairTradingRequest> requests = PairTradingRequest.Values.Where(x => x.LongSymbol == MarketData.Security.Symbol || x.ShortSymbol == MarketData.Security.Symbol).ToList();

                foreach (PairTradingRequest rq in requests)
                {
                    if (rq.LongSymbol == MarketData.Security.Symbol)
                    {
                        rq.LastLong = MarketData.Trade;
                        rq.LastMDLong=MarketData;
                    }

                    if (rq.ShortSymbol == MarketData.Security.Symbol)
                    {
                        rq.LastShort = MarketData.Trade;
                        rq.LastMDShort=MarketData;
                    }

                    if (!rq.Opened)
                    {
                        if (rq.EvalReadyToBeOpened())
                        {
                            //Ruteamos las ordenes
                            rq.LastStatus = string.Format("Opening pair position for symbols LONG {0} - SHORT {1}: Current Spread={2} Trigger Spread={3}", rq.LongSymbol, rq.ShortSymbol, rq.GetCurrentSpread(), rq.SpreadLong);

                            Position posLong = new Position()
                            {
                                QuantityType=QuantityType.SHARES,
                                Side=Side.Buy,
                                Qty = rq.GetPosQty(true),
                                Symbol = rq.LongSymbol,
                                Security = new Security()
                                    {
                                        Symbol = rq.LongSymbol,
                                        MarketData = rq.LastMDLong,
                                        Currency = StrategyConfiguration.Currency,
                                        SecType = SecurityType.CS
                                    }
                            };

                            posLong.LoadPosId(rq.Id);

                            Position posShort = new Position()
                            {
                                QuantityType = QuantityType.SHARES,
                                Side = Side.Sell,
                                Qty = rq.GetPosQty(false),
                                Symbol = rq.ShortSymbol,
                                Security = new Security()
                                {
                                    Symbol = rq.ShortSymbol,
                                    MarketData = rq.LastMDShort,
                                    Currency = StrategyConfiguration.Currency,
                                    SecType = SecurityType.CS
                                }
                            };

                            posShort.LoadPosId(_POS_ID_SPREAD + rq.Id);


                            PositionWrapper posLongWrapper = new PositionWrapper(posLong, Config);
                            PositionWrapper posShortWrapper = new PositionWrapper(posShort, Config);

                            CMState stateLong = OrderRouter.ProcessMessage(posLongWrapper);

                            CMState stateShort = OrderRouter.ProcessMessage(posShortWrapper);

                            if (stateLong.Success && stateShort.Success)
                            {
                                rq.Opened = true;
                                return CMState.BuildSuccess();
                            }
                            else
                                return CMState.BuildFail(stateLong.Exception != null ? stateLong.Exception : (stateShort.Exception != null ? stateShort.Exception : null));

                            OrderRouter.ProcessMessage(wrapper);


                        }
                        else
                            rq.LastStatus = string.Format("Pair position for symbols LONG {0} - SHORT {1} not ready to be opened: Current Spread={2} Trigger Spread={3}", rq.LongSymbol, rq.ShortSymbol, rq.GetCurrentSpread(), rq.SpreadLong);
                    }
                    else//position is opened
                    {
                        OrderRouter.ProcessMessage(wrapper);
                        //TODO: Eval closing
                        // TODO: Actualiar el reporte de estados
                    }
                }

                return CMState.BuildSuccess();
            }
        }

        protected void ProcessExecutionReport(Wrapper erWrapper)
        {
            ExecutionReport report = ExecutionReportConverter.GetExecutionReport(erWrapper, Config);
        
            
            //Vamos a encontrar la posición por el symbol. Pero debería ser por Id en un futuro
            IEnumerable<PairTradingRequest> requests = PairTradingRequest.Values.Where(x => x.LongSymbol == report.Order.Symbol || x.ShortSymbol == report.Order.Symbol).ToList();

            foreach (PairTradingRequest rq in requests) 
            {
             
                if (rq.LongSymbol == report.Order.Symbol)
                {
                    rq.LastERLong = report;
                   

                    string erStatus = string.Format(@"LONG {0}- Exec. Report:  Side={1} Ord. Status={2} Exec. Type={3} CumQty={4} LeavesQty{5} {6}",
                                             report.Order.Symbol, report.Order.Side, report.OrdStatus, report.ExecType, report.CumQty, report.LeavesQty,Environment.NewLine);
                    erStatus += string.Format(@"SHORT {0}- Status={1} CumQty={2} LeavesQty{3}",
                                               rq.ShortSymbol, rq.LastERShort != null ? rq.LastERShort.OrdStatus.ToString() : "",
                                               rq.LastERShort != null ? rq.LastERShort.CumQty.ToString() : "",
                                               rq.LastERShort != null ? rq.LastERShort.LeavesQty.ToString() : ""
                                            );
                    erStatus += string.Format("{0}Last Spread={1}", Environment.NewLine, rq.GetCurrentSpread());
                    rq.LastStatus = erStatus;
                                                                                        
                                                                                              

                }

                if (rq.ShortSymbol == report.Order.Symbol)
                {
                    rq.LastERShort = report;


                    string erStatus = string.Format(@"LONG {0}- Status={1} CumQty={2} LeavesQty{3} {4}",
                                               rq.ShortSymbol, rq.LastERLong != null ? rq.LastERLong.OrdStatus.ToString() : "",
                                               rq.LastERLong != null ? rq.LastERLong.CumQty.ToString() : "",
                                               rq.LastERLong != null ? rq.LastERLong.LeavesQty.ToString() : "",
                                               Environment.NewLine
                                            );
                    erStatus += string.Format(@"SHORT {0}- Exec. Report:  Side={1} Ord. Status={2} Exec. Type={3} CumQty={4} LeavesQty{5}",
                                                report.Order.Symbol, report.Order.Side, report.OrdStatus, report.ExecType, report.CumQty, report.LeavesQty);
                    erStatus += string.Format("{0}Last Spread={1}", Environment.NewLine, rq.GetCurrentSpread());
                    
                    rq.LastStatus = erStatus;
                }


                if (rq.LastERLong.OrdStatus == OrdStatus.Filled && rq.LastERShort.OrdStatus == OrdStatus.Filled)
                    rq.LastStatus = string.Format("Position opened and active in the market. Symbols LONG {0} - SHORT {1}", rq.LongSymbol, rq.ShortSymbol);
            
            }

        }


        #endregion

        #region Public Methods

        public Main.Common.DTO.CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
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
                    //Positions = new Dictionary<string, Position>();
                    ExecutionSummaries = new Dictionary<string, ExecutionSummary>();
                    ExecutionReportConverter = new ExecutionReportConverter();
                    MarketDataConverter = new MarketDataConverter();
                    PositionConverter = new PositionConverter();
                    PairTradingRequest = new Dictionary<int, Common.DTO.PairTradingRequest>();
                    tLock = new object();

                    InitializeRESTService();

                    //ExecutionSummaryManager.GetTest();
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


        #region Communication Methods

        protected PairTradingResponse ProcessPairTradingStatus(int id)
        {

            lock (tLock)
            {
                if (PairTradingRequest.ContainsKey(id))
                {
                    PairTradingRequest rq = PairTradingRequest[id];
                    return new PairTradingResponse() { Response = string.Format(rq.LastStatus) };
                }
                else
                    return new PairTradingResponse() { Response = string.Format("Pair trading position not found for id {0}", id) };
            
            
            }
        
        
        }

        protected PairTradingResponse ProcessPairTradingRequest(PairTradingRequest rq)
        {
            try
            {
                InitializeMergeTrading(rq);
                return new PairTradingResponse() { Response = "Request received" };

            }
            catch (Exception ex)
            {
                return new PairTradingResponse() { Response = "Error receiving request: " + ex.Message };
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
                    DoLog("ProcessOutgoing - Execution Report: " + wrapper.ToString(), Constants.MessageType.Information);
                    ProcessExecutionReport(wrapper);
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        #endregion
    }
}

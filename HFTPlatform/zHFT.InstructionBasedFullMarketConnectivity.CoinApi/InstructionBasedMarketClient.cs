using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.BasedFullMarketConnectivity.Primary.Common;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.MarketClient.Primary.Common.Converters;
using zHFT.MarketClient.Primary.Common.Wrappers;
using zHFT.OrderRouters.Primary.Common;
using zHFT.OrderRouters.Primary.Common.Wrappers;
using zHFT.SingletonModulesHandler.Common.Interfaces;
using zHFT.SingletonModulesHandler.Common.Util;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.SecurityListSaver.Common.Interfaces;

namespace zHFT.InstructionBasedFullMarketConnectivity.CoinApi
{
    public class InstructionBasedMarketClient : BaseFullMarketConnectivity, ISingletonModule
    {
        #region Constructors

        private InstructionBasedMarketClient(OnLogMessage pOnLogMsg, string configFile)
        {
            Initialize(pOnLogMsg, configFile);
        }

        #endregion

        #region Private Static Attributes

        private static InstructionBasedMarketClient Instance { get; set; }

        #endregion

        #region Public Static Methods

        public static InstructionBasedMarketClient GetInstance(OnLogMessage pOnLogMsg, string configFile)
        {
            if (Instance == null)
            {
                Instance = new InstructionBasedMarketClient(pOnLogMsg, configFile);
            }
            return Instance;
        }

        #endregion

        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected ISecurityTranslator SecurityTranslator { get; set; }

        protected Common.Configuration.Configuration CoinApiConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected OnMessageReceived OnMarketDataMessageRcv { get; set; }

        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        private Dictionary<int, Security> SecuritiesToPublish { get; set; }

        private InstructionManager InstructionManager { get; set; }

        private AccountManager AccountManager { get; set; }

        private PositionManager PositionManager { get; set; }

        protected DateTime Start { get; set; }

        protected Thread PublishThread { get; set; }

        protected Thread SaveMarketData { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected Thread MarketDataRequestThread { get; set; }

        #endregion

        #region Quickfix Objects Methods

        protected void ProcesssMDFullRefreshMessage(QuickFix.Message message)
        { 
        
        
        }

        #endregion

        #region Public Methods

        public override BaseConfiguration GetConfig()
        {
            return (BaseConfiguration)CoinApiConfiguration;
        }

        public override void fromApp(QuickFix.Message value, QuickFix.SessionID sessionId)
        {
            
            try
            {
                DoLog("Invocación de fromApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

                if (value is QuickFix44.MarketDataIncrementalRefresh)
                {
                    DoLog(string.Format("{0}: Market Data Incremental Refresh Message received and not processed:{1} ", CoinApiConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                }
                else if (value is QuickFix44.MarketDataSnapshotFullRefresh)
                {
                    QuickFix44.MarketDataSnapshotFullRefresh msg = (QuickFix44.MarketDataSnapshotFullRefresh)value;
                    ProcesssMDFullRefreshMessage(msg);
                }
                else if (value is QuickFix44.SecurityList)
                {
                    //SecurityListWrapper wrapper = new SecurityListWrapper((QuickFix44.SecurityList)value, (IConfiguration)Config);
                    //ProcessSecurityList(wrapper);
                }
                else if (value is QuickFix44.ExecutionReport)
                {
                    QuickFix44.ExecutionReport msg = (QuickFix44.ExecutionReport)value;
                    ExecutionReportWrapper erWrapper = ProcesssExecutionReportMessage(msg);
                    OnExecutionReportMessageRcv(erWrapper);
                }
                else if (value is QuickFix44.MarketDataRequestReject)
                {
                    DoLog(string.Format("{0}: MarketDataRequestReject:{1} ", CoinApiConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                }
                else
                {
                    DoLog(string.Format("{0}: Unknown message:{1} ", CoinApiConfiguration.Name, value.ToString()), Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {

                DoLog(string.Format("{0}: Error processing message @fromApp:{1} ", CoinApiConfiguration.Name, ex.Message), Constants.MessageType.Error);

            }
           
        }

        public override void toAdmin(QuickFix.Message value, QuickFix.SessionID sessionId)
        {
          
            try
            {
                if (value is QuickFixT11.Logon)
                {
                    QuickFixT11.Logon logon = (QuickFixT11.Logon)value;
                    //logon.setField(Username.FIELD, PrimaryConfiguration.User);
                    //logon.setField(Password.FIELD, PrimaryConfiguration.Password);
                    DoLog("Invocación de toAdmin-logon por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                else if (value is QuickFixT11.Reject)
                {
                    QuickFixT11.Reject reject = (QuickFixT11.Reject)value;
                    DoLog("Invocación de toAdmin-reject por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                else
                    DoLog("Invocación de toAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("{0}: Error processing message @toAdmin:{1} ", CoinApiConfiguration.Name, ex.Message), Constants.MessageType.Error);
            }
          
        }

        public CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {

                    if (wrapper.GetAction() == Actions.NEW_ORDER)
                    {
                        DoLog(string.Format("@{0}:Routing to market for symbol {1}", CoinApiConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return RouteNewOrder(wrapper);
                    }
                    else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                    {
                        DoLog(string.Format("@{0}:Updating order for symbol {1}", CoinApiConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        UpdateOrder(wrapper);
                        return CMState.BuildSuccess();

                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                    {
                        DoLog(string.Format("@{0}:Canceling order for ClOrdId {1}", CoinApiConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        CancelOrder(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (wrapper.GetAction() == Actions.MARKET_DATA_REQUEST)
                    {
                        DoLog(string.Format("Receiving Market Data Request: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return ProcessMarketDataRequest(wrapper);
                    }
                    else
                    {

                        Actions action = wrapper.GetAction();
                        DoLog(string.Format("@{0}:Sending message " + action + " not implemented", CoinApiConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message " + action + " not implemented", CoinApiConfiguration.Name)));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");

            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Main.Common.Util.Constants.MessageType.Error);
                throw;
            }
        }

        public bool Initialize(OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnLogMsg += pOnLogMsg;

                if (ConfigLoader.DoLoadConfig(this, configFile))
                {
                    ActiveSecurities = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();
                    OrderConverter = new OrderConverter();
                    SecurityListConverter = new SecurityListConverter();
                    ActiveOrders = new Dictionary<string, Order>();
                    ActiveOrderIdMapper = new Dictionary<string, int>();
                    ReplacingActiveOrderIdMapper = new Dictionary<string, int>();
                    SecuritiesToPublish = new Dictionary<int, Security>();
                    OrderIndexId = GetNextOrderId();
                    Start = DateTime.Now;

                    InstructionManager = new InstructionManager(CoinApiConfiguration.InstructionsAccessLayerConnectionString);
                    PositionManager = new PositionManager(CoinApiConfiguration.InstructionsAccessLayerConnectionString);
                    AccountManager = new AccountManager(CoinApiConfiguration.InstructionsAccessLayerConnectionString);
                    //MarketManager = new MarketManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    //StockManager = new StockManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    //OptionManager = new OptionManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    //StockMarketDataManager = new StockMarkeDataManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);
                    //OptionMarketDataManager = new OptionMarketDataManager(PrimaryConfiguration.SecuritiesAccessLayerConnectionString);


                    var fixMessageCreator = Type.GetType(CoinApiConfiguration.FIXMessageCreator);
                    if (fixMessageCreator != null)
                    {
                        FIXMessageCreator = (IFIXMessageCreator)Activator.CreateInstance(fixMessageCreator);
                    }
                    else
                        throw new Exception(string.Format("@{0}:Assembly not found: " + CoinApiConfiguration.FIXMessageCreator));

                    var typeMarketTranslator = Type.GetType(CoinApiConfiguration.SecuritiesMarketTranslator);
                    if (typeMarketTranslator != null)
                        SecurityTranslator = (ISecurityTranslator)Activator.CreateInstance(typeMarketTranslator);
                    else
                    {
                        DoLog("assembly not found: " + CoinApiConfiguration.SecuritiesMarketTranslator, Main.Common.Util.Constants.MessageType.Error);
                        return false;
                    }

                    SessionSettings = new SessionSettings(CoinApiConfiguration.FIXInitiatorPath);
                    FileStoreFactory = new FileStoreFactory(SessionSettings);
                    ScreenLogFactory = new ScreenLogFactory(SessionSettings);
                    MessageFactory = new DefaultMessageFactory();

                    Initiator = new SocketInitiator(this, FileStoreFactory, SessionSettings, ScreenLogFactory, MessageFactory);

                    Initiator.start();

                    PublishThread = new Thread(DoPublishMarketData);
                    PublishThread.Start();

                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    ProcessInstructionsThread = new Thread(DoFindInstructions);
                    ProcessInstructionsThread.Start();

                    MarketDataRequestThread = new Thread(DoRequestMarketData);
                    MarketDataRequestThread.Start();

                    return true;

                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, CoinApiConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing {1}:{2}", CoinApiConfiguration.Name,
                                                                              configFile,
                                                                              ex.Message),
                                                                              Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion

        #region Protected Methods

        protected void DoRequestMarketData()
        { 
            //TO DO: Implementar
        }


        protected void DoCleanOldSecurities()
        { 
            //TO DO: Implementar
        
        }

        protected void ProcessPositionInstruction(Instruction instr)
        {
            //try
            //{
            //    if (instr != null)
            //    {
            //        if (!ActiveSecurities.Values.Any(x => x.Symbol == instr.Symbol))
            //        {
            //            instr = InstructionManager.GetById(instr.Id);

            //            if (instr.InstructionType.Type == InstructionType._NEW_POSITION || instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
            //            {
            //                Security sec = new Security()
            //                {
            //                    Symbol = instr.Symbol,
            //                    Exchange = ExchangeConverter.GetMarketFromFullSymbol(instr.Symbol),
            //                    SecType = instr.SecurityType
            //                };


            //                ContractsTimeStamps.Add(instr.Id, DateTime.Now);
            //                SecuritiesToPublish.Add(instr.Id, sec);
            //                SecurityTypes.Add(instr.Symbol, instr.SecurityType);

            //                if (!CoinApiConfiguration.RequestFullMarketData)//No tenemos todos los securities
            //                {
            //                    ActiveSecurities.Add(instr.Id, sec);
            //                    MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(sec, zHFT.Main.Common.Enums.SubscriptionRequestType.SnapshotAndUpdates);
            //                    ProcessMarketDataRequest(wrapper);

            //                }
            //            }
            //        }
            //    }
            //    else
            //        throw new Exception(string.Format("@{1}Could not find a related instruction for id {0}", instr.Id, PrimaryConfiguration.Name));
            //}
            //catch (Exception ex)
            //{

            //    DoLog(string.Format("@{2}:Critical error processing related instruction: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            //}
        }

        protected void DoFindInstructions()
        {
            while (true)
            {
                Thread.Sleep(CoinApiConfiguration.SearchForInstructionsInMilliseconds);

                lock (tLock)
                {
                    List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(CoinApiConfiguration.AccountNumber);

                    try
                    {
                        foreach (Instruction instr in instructionsToProcess.Where(x => x.InstructionType.Type == InstructionType._NEW_POSITION || x.InstructionType.Type == InstructionType._UNWIND_POSITION))
                        {
                            //We process the account positions sync instructions
                            ProcessPositionInstruction(instr);
                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{2}:Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), CoinApiConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    }
                }

            }
        }

        protected void RunPublishSecurity(Security sec, IConfiguration Config)
        { 
            //TO DO : Implementar
        
        
        }

        protected void RunSaveMarketData(Object param)
        { 
        
            //TODO: Implementar
        }

        protected void DoPublishMarketData()
        {
            while (true)
            {
                Thread.Sleep(CoinApiConfiguration.PublishUpdateInMilliseconds);

                lock (tLock)
                {
                    foreach (Security sec in ActiveSecurities.Values)
                    {
                        if (SecuritiesToPublish.Values.Any(x => x.Symbol == sec.Symbol))
                            RunPublishSecurity(sec, CoinApiConfiguration);

                        if (CoinApiConfiguration.SaveFullMarketData)
                        {
                            SaveMarketData = new Thread(RunSaveMarketData);
                            SaveMarketData.Start(sec);
                        }
                    }
                }
            }
        }

        protected override void ProcessSecurities(SecurityList securityList)
        {
            //TO DO: Implementar
        }

        #endregion

        #region ISingletonModule Methods

        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        public void SetOutgoingEvent(OnMessageReceived OnMessageRcv)
        {
            OnExecutionReportMessageRcv += OnMessageRcv;
        }

        public void SetIncomingEvent(OnMessageReceived OnMessageRcv)
        {
            OnMarketDataMessageRcv += OnMessageRcv;
        }

        #endregion

        
    }
}

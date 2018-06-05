using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.OrderRouters.Bitstamp.Common.Converters;
using zHFT.SingletonModulesHandler.Common.Interfaces;
using zHFT.SingletonModulesHandler.Common.Util;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.SecurityListSaver.Common.Interfaces;

namespace zHFT.InstructionBasedFullMarketConnectivity.Bitstamp
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

        protected Common.Configuration.Configuration BitstampConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        protected OrderConverter OrderConverter { get; set; }

        private Dictionary<int, Security> SecuritiesToPublish { get; set; }

        private InstructionManager InstructionManager { get; set; }

        private AccountManager AccountManager { get; set; }

        private PositionManager PositionManager { get; set; }

        protected Thread MarketDataRequestThread { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread PublishThread { get; set; }

        protected Thread SaveMarketData { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected OnMessageReceived OnMarketDataMessageRcv { get; set; }

        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        protected DateTime Start { get; set; }

        #endregion

        #region Overriden Methods

        public override Main.Common.Abstract.BaseConfiguration GetConfig()
        {
            return (BaseConfiguration)BitstampConfiguration; 
        }

        protected override void ProcessSecurities(zHFT.Main.BusinessEntities.Security_List.SecurityList securityList)
        {
           //TODO: Implementar ProcessSecurities
        }

        protected override void CancelMarketData(zHFT.Main.BusinessEntities.Securities.Security sec)
        {
            //TODO: Implementar CancelMarketData
        }

        #endregion

        #region Protected Methods

        protected void DoPublishMarketData()
        { 
            //TODO : Implementar thread de publicación de Market Data
        }

        protected void DoCleanOldSecurities()
        { 
            //TODO: Implementar thread de limpieza de viejos securities
        }

        protected void DoFindInstructions()
        { 
            //TODO: Implementar thread de rastreo de instrucciones
        }

        protected void DoRequestMarketData()
        { 
            //TODO: Implementar thread de request de Market Data
        }

        #endregion

        #region Public Methods

        public CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {

                    if (wrapper.GetAction() == Actions.NEW_ORDER)
                    {
                        DoLog(string.Format("@{0}:Routing with Bitstamp to market for symbol {1}", BitstampConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        //return RouteNewOrder(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                    {
                        DoLog(string.Format("@{0}:Updating order with Bitstamp  for symbol {1}", BitstampConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        //UpdateOrder(wrapper);
                        return CMState.BuildSuccess();

                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                    {
                        DoLog(string.Format("@{0}:Canceling order with Bitstamp  for ClOrdId {1}", BitstampConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        //CancelOrder(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (wrapper.GetAction() == Actions.MARKET_DATA_REQUEST)
                    {
                        DoLog(string.Format("Receiving Market Data Request: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        //return ProcessMarketDataRequest(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                    {
                        DoLog(string.Format("@{0}:Cancelling all active orders @ IB", BitstampConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        //return CancelAllOrders();
                        return CMState.BuildSuccess();
                    }
                    else
                    {

                        Actions action = wrapper.GetAction();
                        DoLog(string.Format("@{0}:Sending message " + action + " not implemented", BitstampConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message " + action + " not implemented", BitstampConfiguration.Name)));
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

                    InstructionManager = new InstructionManager(BitstampConfiguration.InstructionsAccessLayerConnectionString);
                    PositionManager = new PositionManager(BitstampConfiguration.InstructionsAccessLayerConnectionString);
                    AccountManager = new AccountManager(BitstampConfiguration.InstructionsAccessLayerConnectionString);

                    var fixMessageCreator = Type.GetType(BitstampConfiguration.FIXMessageCreator);
                    if (fixMessageCreator != null)
                    {
                        FIXMessageCreator = (IFIXMessageCreator)Activator.CreateInstance(fixMessageCreator);
                    }
                    else
                        throw new Exception(string.Format("@{0}:Assembly not found: " + BitstampConfiguration.FIXMessageCreator));

                    SessionSettings = new SessionSettings(BitstampConfiguration.FIXInitiatorPath);
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
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, BitstampConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing {1}:{2}", BitstampConfiguration.Name,
                                                                              configFile,
                                                                              ex.Message),
                                                                              Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion

        #region QuickFix Methods

        public override void fromApp(Message value, SessionID sessionId)
        {
            try
            {
                DoLog("Invocación de fromApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

                if (value is QuickFix44.MarketDataIncrementalRefresh)
                {
                    DoLog(string.Format("{0}: Market Data Incremental Refresh Message received and not processed:{1} ", BitstampConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                }
                else if (value is QuickFix44.MarketDataSnapshotFullRefresh)
                {
                    QuickFix44.MarketDataSnapshotFullRefresh msg = (QuickFix44.MarketDataSnapshotFullRefresh)value;
                    //ProcesssMDFullRefreshMessage(msg);
                }
                else if (value is QuickFix44.SecurityList)
                {
                    //SecurityListWrapper wrapper = new SecurityListWrapper((QuickFix50.SecurityList)value, (IConfiguration)Config);
                    //ProcessSecurityList(wrapper);
                }
                else if (value is QuickFix44.ExecutionReport)
                {
                    //QuickFix50.ExecutionReport msg = (QuickFix50.ExecutionReport)value;
                    //ExecutionReportWrapper erWrapper = ProcesssExecutionReportMessage(msg);
                    //OnExecutionReportMessageRcv(erWrapper);
                }
                else if (value is QuickFix44.MarketDataRequestReject)
                {
                    DoLog(string.Format("{0}: MarketDataRequestReject:{1} ", BitstampConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                }
                else
                {
                    DoLog(string.Format("{0}: Unknown message:{1} ", BitstampConfiguration.Name, value.ToString()), Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {

                DoLog(string.Format("{0}: Error processing message @fromApp:{1} ", BitstampConfiguration.Name, ex.Message), Constants.MessageType.Error);

            }

        }

        public override void toAdmin(Message value, SessionID sessionId)
        {
            try
            {
                if (value is QuickFix44.Logon)
                {
                    QuickFix44.Logon logon = (QuickFix44.Logon)value;
                    logon.setField(Username.FIELD, BitstampConfiguration.User);
                    logon.setField(Password.FIELD, BitstampConfiguration.Password);
                    DoLog("Invocación de toAdmin-logon por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                else if (value is QuickFix44.Reject)
                {
                    QuickFix44.Reject reject = (QuickFix44.Reject)value;
                    DoLog("Invocación de toAdmin-reject por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                else
                    DoLog("Invocación de toAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("{0}: Error processing message @toAdmin:{1} ", BitstampConfiguration.Name, ex.Message), Constants.MessageType.Error);
            }

        }

        public override void onLogon(SessionID value)
        {
            SessionID = value;
            DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

            if (SessionID != null)
            {
                if (BitstampConfiguration.RequestSecurityList)
                {
                    //SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(zHFT.Main.Common.Enums.SecurityListRequestType.AllSecurities, null);
                    //ProcessSecurityListRequest(slWrapper);
                }
                DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);

            }
            else
                DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);
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

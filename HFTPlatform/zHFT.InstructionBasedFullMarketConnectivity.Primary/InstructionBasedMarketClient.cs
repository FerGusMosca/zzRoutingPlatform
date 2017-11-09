using QuickFix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.FixMessageCreator.Primary.Common.v50Sp2;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;
using zHFT.SingletonModulesHandler.Common.Interfaces;
using zHFT.SingletonModulesHandler.Common.Util;

namespace zHFT.InstructionBasedFullMarketConnectivity.Primary
{
    public class InstructionBasedMarketClient : Application, ISingletonModule
    {
        #region Constructors

        private InstructionBasedMarketClient(OnLogMessage pOnLogMsg, string configFile)
        {
            Initialize(pOnLogMsg, configFile);
        }

        #endregion

        #region Private  Consts

        private int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        private int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        private string _DUMMY_SECURITY = "kcdlsncslkd";

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

        protected Common.Configuration.Configuration PrimaryConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private IFIXMessageCreator FIXMessageCreator { get; set; }

        private InstructionManager InstructionManager { get; set; }

        private AccountManager AccountManager { get; set; }

        private PositionManager PositionManager { get; set; }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        protected SessionSettings SessionSettings { get; set; }
        protected FileStoreFactory FileStoreFactory { get; set; }
        protected ScreenLogFactory ScreenLogFactory { get; set; }
        protected SessionID SessionID { get; set; }
        protected MessageFactory MessageFactory { get; set; }
        protected SocketInitiator Initiator { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread PublishThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected OnMessageReceived OnMarketDataMessageRcv { get; set; }

        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected object tLock = new object();

        #endregion

        #region Quickfix Objects Methods

        protected bool RequestMarketData(Instruction instr)
        {
            try
            {
                if (SessionID != null)
                {
                    string symbol = SecurityConverter.GetSymbolToPrimary(instr.Symbol, PrimaryConfiguration.Market, PrimaryConfiguration.MarketPrefixCode, PrimaryConfiguration.MarketClearingID);
                    QuickFix.Message mdRequest = FIXMessageCreator.RequestMarketData(instr.Id, symbol);


                    DoLog(string.Format("@{0}:Sending message: {1}", PrimaryConfiguration.Name, mdRequest.ToString()),
                            Main.Common.Util.Constants.MessageType.Information);

                    Session.sendToTarget(mdRequest, SessionID);
                    return true;
                }
                else
                {

                    DoLog(string.Format("@{0}:Could not request market data for null SessionID", PrimaryConfiguration.Name),
                          Main.Common.Util.Constants.MessageType.Error);
                    return false;

                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error @RequestMarketData: {1}", PrimaryConfiguration.Name, ex.Message),
                      Main.Common.Util.Constants.MessageType.Error);
                return false;

            }
        }
    
        protected void ProcesssMDFullRefreshMessage(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", PrimaryConfiguration.Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            string symbol = message.getField(Symbol.FIELD);

            if (symbol != null)
            {
                symbol = SecurityConverter.GetCleanSymbolFromPrimary(symbol);

                if (ActiveSecurities.Values.Any(x => x.Symbol == symbol))
                {
                    Security sec = ActiveSecurities.Values.Where(x => x.Symbol == symbol).FirstOrDefault();

                    FIXMessageCreator.ProcessMarketData(message, sec, OnLogMsg);
                }
            }
            else
            {
                if (symbol != null)
                    DoLog(string.Format("@{0}:Unknown market data for symbol {1} ", PrimaryConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Error);
                else
                    DoLog(string.Format("@{0}:Market data with no symbol", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        #endregion

        #region Protected Methods

        protected void ProcessPositionInstruction(Instruction instr)
        {
            try
            {
                if (instr != null)
                {
                    if (!ActiveSecurities.Keys.Contains(instr.Id))
                    {
                        instr = InstructionManager.GetById(instr.Id);

                        if (instr.InstructionType.Type == InstructionType._NEW_POSITION || instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
                        {
                            if (RequestMarketData(instr))
                            {

                                Security sec = new Security()
                                {
                                    Symbol = SecurityConverter.GetCleanSymbolFromFullSymbol(instr.Symbol, PrimaryConfiguration.Market),
                                };

                                ActiveSecurities.Add(instr.Id, sec);
                                ContractsTimeStamps.Add(instr.Id, DateTime.Now);
                            }
                        }
                    }
                }
                else
                    throw new Exception(string.Format("@{1}Could not find a related instruction for id {0}", instr.Id, PrimaryConfiguration.Name));
            }
            catch (Exception ex)
            {

                DoLog(string.Format("@{2}:Critical error processing related instruction: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void DoFindInstructions()
        {
            while (true)
            {
                Thread.Sleep(PrimaryConfiguration.SearchForInstructionsInMilliseconds);

                lock (tLock)
                {
                    List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(PrimaryConfiguration.AccountNumber);

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
                        DoLog(string.Format("@{2}:Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    }
                }

            }
        }

        protected void RunPublishSecurity(Security sec, IConfiguration Config)
        {
            try
            {
                zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper wrapper = new zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper(sec,PrimaryConfiguration.Market, Config);
                CMState state = OnMarketDataMessageRcv(wrapper);

                if (state.Success)
                    DoLog(string.Format("@{1}:Publishing Market Data for Security {0} ", sec.Symbol, PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                else
                    DoLog(string.Format("@{2}:Error Publishing Market Data for Security {0}. Error={1} ",
                                        sec.Symbol,
                                        state.Exception != null ? state.Exception.Message : "",
                                        PrimaryConfiguration.Name),
                                        Main.Common.Util.Constants.MessageType.Error);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{2}:Error Publishing Market Data for Security {0}. Error={1} ",
                                            sec.Symbol, ex != null ? ex.Message : "",
                                            PrimaryConfiguration.Name),
                                            Main.Common.Util.Constants.MessageType.Error);
            }

        }

        protected void DoCleanOldSecurities()
        {
            while (true)
            {
                Thread.Sleep(_SECURITIES_REMOVEL_PERIOD);//Once every hour

                lock (tLock)
                {
                    try
                    {
                        List<int> keysToRemove = new List<int>();
                        foreach (int key in ContractsTimeStamps.Keys)
                        {
                            DateTime timeStamp = ContractsTimeStamps[key];

                            if ((DateTime.Now - timeStamp).Hours >= _MAX_ELAPSED_HOURS_FOR_MARKET_DATA)
                            {
                                keysToRemove.Add(key);
                            }
                        }

                        foreach (int keyToRemove in keysToRemove)
                        {
                            ContractsTimeStamps.Remove(keyToRemove);
                            ActiveSecurities.Remove(keyToRemove);
                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{1}:There was an error cleaning old securities from market data flow error={0} ",
                              ex.Message, PrimaryConfiguration.Name),
                              Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        protected void DoPublishMarketData()
        {
            while (true)
            {
                Thread.Sleep(PrimaryConfiguration.PublishUpdateInMilliseconds);

                lock (tLock)
                {
                    foreach (Security sec in ActiveSecurities.Values)
                    {
                        RunPublishSecurity(sec, PrimaryConfiguration);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public  CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {

                    if (wrapper.GetAction() == Actions.NEW_ORDER)
                    {
                        DoLog(string.Format("@{0}:Routing with Primary to market for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        //RouteNewOrder(wrapper);
                        return CMState.BuildSuccess();

                    }
                    else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                    {
                        DoLog(string.Format("@{0}:Updating order with Primary  for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        //UpdateOrder(wrapper, false);
                        return CMState.BuildSuccess();

                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                    {
                        DoLog(string.Format("@{0}:Canceling order with Primary  for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        //UpdateOrder(wrapper, true);
                        return CMState.BuildSuccess();
                    }
                    else
                    {

                        Actions action = wrapper.GetAction();
                        DoLog(string.Format("@{0}:Sending message " + action + " not implemented", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message " + action + " not implemented", PrimaryConfiguration.Name)));
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

        public  bool Initialize(OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnLogMsg += pOnLogMsg;

                if (ConfigLoader.DoLoadConfig(this,configFile))
                {
                    ActiveSecurities = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();

                    InstructionManager = new InstructionManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);
                    PositionManager = new PositionManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);
                    AccountManager = new AccountManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);

                    var fixMessageCreator = Type.GetType(PrimaryConfiguration.FIXMessageCreator);
                    if (fixMessageCreator != null)
                    {
                        FIXMessageCreator = (IFIXMessageCreator)Activator.CreateInstance(fixMessageCreator);
                    }
                    else
                        throw new Exception(string.Format("@{0}:Assembly not found: " + PrimaryConfiguration.FIXMessageCreator));


                    SessionSettings = new SessionSettings(PrimaryConfiguration.FIXInitiatorPath);
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

                    return true;

                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing {1}:{2}", PrimaryConfiguration.Name,
                                                                              configFile,
                                                                              ex.Message),
                                                                              Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion

        #region QuickFix Methods

        public void fromAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de fromAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void fromApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                try
                {
                    DoLog("Invocación de fromApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

                    if (value is QuickFix50.MarketDataIncrementalRefresh)
                    {
                        DoLog(string.Format("{0}: Market Data Incremental Refresh Message received and not processed:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Error);

                    }
                    else if (value is QuickFix50.MarketDataSnapshotFullRefresh)
                    {
                        QuickFix50.MarketDataSnapshotFullRefresh msg = (QuickFix50.MarketDataSnapshotFullRefresh)value;
                        ProcesssMDFullRefreshMessage(msg);

                    }
                    else if (value is QuickFix50Sp2.MarketDataRequestReject)
                    {
                        DoLog(string.Format("{0}: MarketDataRequestReject:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                    }
                    else
                    {
                        DoLog(string.Format("{0}: Unknown message:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Information);
                    }
                }
                catch (Exception ex)
                {

                    DoLog(string.Format("{0}: Error processing message @fromApp:{1} ", PrimaryConfiguration.Name, ex.Message), Constants.MessageType.Error);

                }
            }
        }

        public void onCreate(SessionID value)
        {
            lock (tLock)
            {
                DoLog("Invocación de onCreate : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void onLogon(SessionID value)
        {
            lock (tLock)
            {
                SessionID = value;
                DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

                if (SessionID != null)
                    DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);
                else
                    DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);
            }
        }

        public void onLogout(SessionID value)
        {
            lock (tLock)
            {
                SessionID = null;
                DoLog("Invocación de onLogout : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void toAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                try
                {
                    if (value is QuickFixT11.Logon)
                    {
                        QuickFixT11.Logon logon = (QuickFixT11.Logon)value;
                        logon.setField(Username.FIELD, PrimaryConfiguration.User);
                        logon.setField(Password.FIELD, PrimaryConfiguration.Password);
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
                    DoLog(string.Format("{0}: Error processing message @toAdmin:{1} ", PrimaryConfiguration.Name, ex.Message), Constants.MessageType.Error);
                }
            }
        }

        public void toApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de toApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
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

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        #endregion
    }
}

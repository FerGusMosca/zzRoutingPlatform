using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers;

namespace zHFT.InstructionBasedMarketClient.Primary.Client
{
    public class PrimaryInstructionBasedMarketClient : BaseCommunicationModule, Application
    {

        #region Private  Consts

        private int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        private int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        #endregion

        #region Private Consts

        private string _DUMMY_SECURITY = "kcdlsncslkd";

        #endregion

        #region Private Attributes

        public IConfiguration Config { get; set; }

        protected InstructionBasedMarketClient.Primary.Common.Configuration.Configuration PrimaryConfiguration
        {
            get { return (InstructionBasedMarketClient.Primary.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

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

        #endregion

        #region Public Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new InstructionBasedMarketClient.Primary.Common.Configuration.Configuration().GetConfiguration<InstructionBasedMarketClient.Primary.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        public override CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    DoLog(string.Format("@{0}:Sending message " + action + " not implemented",PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);


                    return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message " + action + " not implemented",PrimaryConfiguration.Name)));
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
                            //Suscribirse al Market Data @QuickFix!
                            //ActiveSecurities.Add(instr.Id, BuildSecurityFromConfig(ctr));
                            //ContractsTimeStamps.Add(instr.Id, DateTime.Now);
                        }
                    }
                }
                else
                    throw new Exception(string.Format("@{1}Could not find a related instruction for id {0}", instr.Id,PrimaryConfiguration.Name));
            }
            catch (Exception ex)
            {

                DoLog(string.Format("@{2}:Critical error processing related instruction: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""),PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
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
                        DoLog(string.Format("@{2}:Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""),PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    }
                }

            }
        }

        protected void RunPublishSecurity(Security sec, IConfiguration Config)
        {
            try
            {
                MarketDataWrapper wrapper = new MarketDataWrapper(sec, Config);
                CMState state = OnMessageRcv(wrapper);

                if (state.Success)
                    DoLog(string.Format("@{1}:Publishing Market Data for Security {0} ", sec.Symbol,PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
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
                              ex.Message,PrimaryConfiguration.Name),
                              Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        protected void DoPublish()
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

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    ActiveSecurities = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();
      
                    InstructionManager = new InstructionManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);
                    PositionManager = new PositionManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);
                    AccountManager = new AccountManager(PrimaryConfiguration.InstructionsAccessLayerConnectionString);

                    SessionSettings = new SessionSettings(PrimaryConfiguration.FIXInitiatorPath);
                    FileStoreFactory = new FileStoreFactory(SessionSettings);
                    ScreenLogFactory = new ScreenLogFactory(SessionSettings);
                    MessageFactory = new DefaultMessageFactory();

                    Initiator = new SocketInitiator(this, FileStoreFactory, SessionSettings, ScreenLogFactory, MessageFactory);

                    Initiator.start();

                    PublishThread = new Thread(DoPublish);
                    PublishThread.Start();

                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    ProcessInstructionsThread = new Thread(DoFindInstructions);
                    ProcessInstructionsThread.Start();

                    return true;

                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile,PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
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
                DoLog("Invocación de fromApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

                if (value is QuickFix50.MarketDataIncrementalRefresh)
                {
                   

                }
                else if (value is QuickFix50.MarketDataSnapshotFullRefresh)
                {


                }
                //TODO: Suscription response
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
        }

        public void toApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de toApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        #endregion
    }
}

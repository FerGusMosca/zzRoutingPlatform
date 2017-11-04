using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.DataAccessLayer.Managers;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.MarketClient.IB.Common;
using zHFT.MarketClient.IB.Common.Converters;

namespace zHFT.InstructionBasedMarketClient.IB.Client
{
    public class IBInstructionBasedMarketClient : IBMarketClientBase
    {
        #region Private  Consts

        private int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        private int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        #endregion

        #region Private Attributes

        public IConfiguration Config { get; set; }

        protected InstructionBasedMarketClient.Common.Configuration.Configuration IBConfiguration
        {
            get { return (InstructionBasedMarketClient.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }


        protected Thread PublishThread { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        protected Thread DebugThread { get; set; }

        private InstructionManager InstructionManager { get; set; }

        private AccountManager AccountManager { get; set; }

        private PositionManager PositionManager { get; set; }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        protected object tLock = new object();

        #endregion

        #region Constructors

        public IBInstructionBasedMarketClient() { }

        #endregion

        #region Protected Methods

        protected override IConfiguration GetConfig() { return Config; }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new InstructionBasedMarketClient.Common.Configuration.Configuration().GetConfiguration<InstructionBasedMarketClient.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected void DoPublish()
        {
            while (true)
            {
                Thread.Sleep(IBConfiguration.PublishUpdateInMilliseconds);

                lock (tLock)
                {
                    foreach (Security sec in ActiveSecurities.Values)
                    {
                        RunPublishSecurity(sec, IBConfiguration);
                    }
                }

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
                        DoLog(string.Format("There was an error cleaning old securities from market data flow error={0} ", ex.Message), 
                              Main.Common.Util.Constants.MessageType.Error);
                    }
                }
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
                            zHFT.MarketClient.IB.Common.Configuration.Contract ctr = new MarketClient.IB.Common.Configuration.Contract();

                            ctr.Currency = instr.Account.IBCurrency;
                            ctr.Exchange = IBConfiguration.Exchange;
                            ctr.SecType = zHFT.InstructionBasedMarketClient.IB.Common.Converters.SecurityConverter.GetSecurityType(instr.SecurityType);
                            ctr.Symbol = instr.Symbol;

                            ActiveSecurities.Add(instr.Id, BuildSecurityFromConfig(ctr));
                            ContractsTimeStamps.Add(instr.Id, DateTime.Now);

                            ReqMktData(instr.Id, ctr);

                        }
                    }
                }
                else
                    throw new Exception(string.Format("Could not find a related instruction for id {0}", instr.Id));


            }
            catch (Exception ex)
            {

                DoLog(string.Format("Critical error processing related instruction: {0} - {1}", ex.Message,( ex.InnerException != null ? ex.InnerException.Message : "")), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void DoFindInstructions()
        {
            while (true)
            {
                Thread.Sleep(IBConfiguration.PublishUpdateInMilliseconds);

                lock (tLock)
                {
                    List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(IBConfiguration.AccountNumber);

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
                        DoLog(string.Format("Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : "")), Main.Common.Util.Constants.MessageType.Error);
                    }
                }

            }
        }

        protected override void ProcessField(string ev, int tickerId, int field, double value)
        {
            try
            {
                lock (tLock)
                {
                    if (ActiveSecurities.ContainsKey(tickerId))
                    {

                        Security sec = ActiveSecurities[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);

                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }

        }

        protected override void ProcessField(string ev, int tickerId, int field, int value)
        {
            try
            {
                lock (tLock)
                {
                    if (ActiveSecurities.ContainsKey(tickerId))
                    {
                        Security sec = ActiveSecurities[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);

                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }

        }

        protected override void ProcessField(string ev, int tickerId, int field, string value)
        {
            try
            {
                lock (tLock)
                {
                    if (ActiveSecurities.ContainsKey(tickerId))
                    {
                        Security sec = ActiveSecurities[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Main.Common.Util.Constants.MessageType.Error);

                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }

        }

        #endregion

        #region Public Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string moduleConfigFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(moduleConfigFile))
                {
                    ActiveSecurities = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();

                    ClientSocket = new EClientSocket(this);
                    ClientSocket.eConnect(IBConfiguration.IP, IBConfiguration.Port, IBConfiguration.IdIBClient);

                    InstructionManager = new InstructionManager(IBConfiguration.InstructionsAccessLayerConnectionString);
                    PositionManager = new PositionManager(IBConfiguration.InstructionsAccessLayerConnectionString);
                    AccountManager = new AccountManager(IBConfiguration.InstructionsAccessLayerConnectionString);

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
                    DoLog("Error initializing config file " + moduleConfigFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + moduleConfigFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    DoLog("Sending message " + action + " not implemented", Main.Common.Util.Constants.MessageType.Information);


                    return CMState.BuildFail(new Exception("Sending message " + action + " not implemented"));
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

        #endregion
    }
}

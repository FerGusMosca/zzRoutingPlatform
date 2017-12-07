using Bittrex;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.Bittrex.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Bittrex.Common.DTO;
using zHFT.InstructionBasedMarketClient.Bittrex.DataAccessLayer.Managers;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Converters;
using zHFT.MarketClient.Common.Wrappers;


namespace zHFT.InstructionBasedMarketClient.Bittrex.Client
{
    public class BittrexInstructionBasedMarketClient : BaseCommunicationModule
    {
        #region Private  Consts

        private int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        private int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        #endregion

        #region Protected Attributes

        public Exchange Exchange { get; set; }

        public ExchangeContext ExchangeContext { get; set; }

        protected Bittrex.Common.Configuration.Configuration BittrexConfiguration
        {
            get { return (Bittrex.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        private Dictionary<string, bool> ReverseCurrency { get; set; }

        protected Thread ProcessInstructionsThread { get; set; }

        protected Thread RequestMarketDataThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        private InstructionManager InstructionManager { get; set; }

        private AccountManager AccountManager { get; set; }

        private AccountBittrexDataManager AccountBittrexDataManager { get; set; }

        #endregion

        #region Protected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Bittrex.Common.Configuration.Configuration().GetConfiguration<Bittrex.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected ExchangeContext GetContext()
        {
            return new ExchangeContext()
            {
                ApiKey = BittrexConfiguration.ApiKey,
                QuoteCurrency = BittrexConfiguration.QuoteCurrency,
                Secret = BittrexConfiguration.Secret,
                Simulate = BittrexConfiguration.Simulate
            };
        }

        protected void RemoveSymbol(string symbol)
        {
            List<int> keysToRemove = new List<int>();

            foreach (int key in ActiveSecurities.Keys)
            {
                Security sec = ActiveSecurities[key];

                if (sec.Symbol==symbol)
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
                        DoLog(string.Format("@{1}: There was an error cleaning old securities from market data flow error={0} ", ex.Message, BittrexConfiguration.Name),
                              Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        protected Security BuildSecurityFromInstruction(Instruction instrx)
        {

            Security sec = new Security()
            {
                Symbol = instrx.Symbol,
                SecType = SecurityType.CC
            };

            return sec;

        
        }

        protected void ProcessPositionInstruction(Instruction instr)
        {
            try
            {
                if (instr != null)
                {
                    if (!ActiveSecurities.Keys.Contains(instr.Id)
                        && !ActiveSecurities.Values.Where(x=>x.Active).Any(x => x.Symbol == instr.Symbol))
                    {
                        instr = InstructionManager.GetById(instr.Id);

                        if (instr.InstructionType.Type == InstructionType._NEW_POSITION || instr.InstructionType.Type == InstructionType._UNWIND_POSITION)
                        {
                            ActiveSecurities.Add(instr.Id, BuildSecurityFromInstruction(instr));
                            RequestMarketDataThread = new Thread(DoRequestMarketData);
                            RequestMarketDataThread.Start(instr);
                        }
                    }
                }
                else
                    throw new Exception(string.Format("Could not find a related instruction for id {0}", instr.Id));


            }
            catch (Exception ex)
            {

                DoLog(string.Format("Critical error processing related instruction: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : "")), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        private void ReverseRequestMarketData(Instruction instrx)
        {
            Exchange exch = new Exchange();
            ExchangeContext ctx = GetContext();
            ctx.QuoteCurrency = instrx.Symbol;
            exch.Initialise(ctx);

            JObject jMarketData = exch.GetTicker(BittrexConfiguration.QuoteCurrency);

            Security sec = new Security();
            sec.Symbol = instrx.Symbol;
            sec.MarketData.BestBidPrice = (double?)jMarketData["Bid"];
            sec.MarketData.BestAskPrice =  (double?)jMarketData["Ask"];
            sec.MarketData.Trade = (double?)jMarketData["Last"];
            sec.ReverseMarketData = true;
            MarketDataWrapper wrapper = new MarketDataWrapper(sec, BittrexConfiguration);

            OnMessageRcv(wrapper);
        }

        protected void DoRequestMarketData(Object param)
        {
            Instruction instrx = (Instruction)param;
            try
            {
                DoLog(string.Format("@{0}:Requesting market data por symbol {1}", BittrexConfiguration.Name,instrx.Symbol), Main.Common.Util.Constants.MessageType.Information);

                bool activo = true;
                while (activo)
                {
                    Thread.Sleep(BittrexConfiguration.PublishUpdateInMilliseconds);

                    lock (tLock)
                    {
                        if (ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == instrx.Symbol))
                        {
                            try
                            {
                                Exchange exch = new Exchange();
                                ExchangeContext ctx = GetContext();
                                exch.Initialise(ctx);

                                //Probamos la versión derecha del mercado
                                try
                                {
                                    if (!ReverseCurrency.Keys.Contains(instrx.Symbol))
                                    {
                                        JObject jMarketData = exch.GetTicker(instrx.Symbol);

                                        Security sec = new Security();
                                        sec.Symbol = instrx.Symbol;
                                        sec.MarketData.BestBidPrice = (double?)jMarketData["Bid"];
                                        sec.MarketData.BestAskPrice = (double?)jMarketData["Ask"];
                                        sec.MarketData.Trade = (double?)jMarketData["Last"];
                                        sec.ReverseMarketData = false;
                                        MarketDataWrapper wrapper = new MarketDataWrapper(sec, BittrexConfiguration);

                                        OnMessageRcv(wrapper);
                                    }
                                    else
                                        ReverseRequestMarketData(instrx);
                                }
                                catch (Exception ex)
                                {
                                    ReverseCurrency.Add(instrx.Symbol, true);
                                    ReverseRequestMarketData(instrx);
                                }
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("@{0}:Error Requesting market data por symbol {1}:{2}",
                                        BittrexConfiguration.Name, instrx.Symbol, ex.Message), Main.Common.Util.Constants.MessageType.Information);
                                activo = false;
                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}:Unsubscribing market data por symbol {1}", BittrexConfiguration.Name, instrx.Symbol), Main.Common.Util.Constants.MessageType.Information);

                            activo = false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                lock (tLock)
                {
                    RemoveSymbol(instrx.Symbol);
                }

                DoLog(string.Format("@{0}: Error Requesting market data por symbol {1}:{2}", BittrexConfiguration.Name, instrx.Symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        
        }

        protected void DoFindInstructions()
        {
            while (true)
            {
                Thread.Sleep(BittrexConfiguration.SearchForInstructionsInMilliseconds);

                lock (tLock)
                {
                    List<Instruction> instructionsToProcess = InstructionManager.GetPendingInstructions(BittrexConfiguration.AccountNumber);

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
                        DoLog(string.Format("@{2}:Critical error processing instructions: {0} - {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""), BittrexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    }
                }
            }
        }

        protected void CancelMarketData(Security sec)
        {
            if (ActiveSecurities.Values.Any(x => x.Symbol == sec.Symbol))
            {
                Security toUnsubscribe = ActiveSecurities.Values.Where(x => x.Symbol == sec.Symbol).FirstOrDefault();
                toUnsubscribe.Active = false;
                DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data On Demand for Symbol: {0}", BittrexConfiguration.Name, sec.Symbol), Main.Common.Util.Constants.MessageType.Information);
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe for symbol {1}", BittrexConfiguration.Name, sec.Symbol));

        }

        protected CMState ProessMarketDataRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot not implemented for symbol {1}", BittrexConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot+updates not implemented for symbol {1}", BittrexConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketData(mdr.Security);
                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", BittrexConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected void ConfigBittrexData()
        {
            Account account = AccountManager.GetByAccountNumber(BittrexConfiguration.AccountNumber);

            if (account == null)
                throw new Exception(string.Format("No se encontró ninguna cuenta para el número {0}", BittrexConfiguration.AccountNumber));

            AccountBittrexData bittrexData = AccountBittrexDataManager.GetByAccountNumber(account);

            if (bittrexData == null)
                throw new Exception(string.Format("No se encontró ninguna configuración bittrex para la cuenta número {0}", BittrexConfiguration.AccountNumber));


            BittrexConfiguration.ApiKey = bittrexData.APIKey;
            BittrexConfiguration.Secret = bittrexData.Secret;
        }

        #endregion

        #region Public Methods

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (action == Actions.SECURITY_LIST_REQUEST)
                    {
                        return CMState.BuildSuccess();
                    }
                    else if (Actions.MARKET_DATA_REQUEST == action)
                    {
                        return ProessMarketDataRequest(wrapper);
                    }
                    else
                    {
                        DoLog("Sending message " + action + " not implemented", Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception("Sending message " + action + " not implemented"));
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
                    ReverseCurrency = new Dictionary<string, bool>();

                    AccountManager = new AccountManager(BittrexConfiguration.InstructionsAccessLayerConnectionString);
                    InstructionManager = new InstructionManager(BittrexConfiguration.InstructionsAccessLayerConnectionString,AccountManager);
                    AccountBittrexDataManager = new AccountBittrexDataManager(BittrexConfiguration.InstructionsAccessLayerConnectionString);

                    ConfigBittrexData();

                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    ProcessInstructionsThread = new Thread(DoFindInstructions);
                    ProcessInstructionsThread.Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}

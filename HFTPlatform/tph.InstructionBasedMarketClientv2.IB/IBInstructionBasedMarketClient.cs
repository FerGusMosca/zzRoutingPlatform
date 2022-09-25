using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IBApi;
using tph.MarketClient.IB.Common;
using zHFT.InstructionBasedMarketClient.IB.Common.Converters;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Converters;
using Constants = zHFT.Main.Common.Util.Constants;


namespace tph.InstructionBasedMarketClientv2.IB.Client
{
    public class IBInstructionBasedMarketClient : IBMarketClientBase
    {
        #region Private  Consts

        private int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        private int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        private int _MARKET_DATA_ON_DEMAND_INDEX = 1000000;

        private string _SECURITY_SYMBOL_SEP_ORIG = "$";
        private string _SECURITY_SYMBOL_SEP_DEST = "";

        #endregion

        #region Private Attributes

        public IConfiguration Config { get; set; }

        protected zHFT.InstructionBasedMarketClient.Common.Configuration.Configuration IBConfiguration
        {
            get { return (zHFT.InstructionBasedMarketClient.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected Thread PublishThread { get; set; }

        protected Thread CleanOldSecuritiesThread { get; set; }

        private Dictionary<int, Security> ActiveSecurities { get; set; }

        private Dictionary<int, Security> ActiveSecuritiesOnDemand { get; set; }

        private Dictionary<int, DateTime> ContractsTimeStamps { get; set; }

        protected object tLock = new object();

        #endregion

        #region Protected Methods

        protected override IConfiguration GetConfig() { return Config; }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.InstructionBasedMarketClient.Common.Configuration.Configuration().GetConfiguration<zHFT.InstructionBasedMarketClient.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected void DoPublish()
        {
            while (true)
            {
                Thread.Sleep(IBConfiguration.PublishUpdateInMilliseconds);

                lock (tLock)
                {
                    try
                    {
                        List<int> keysToRemove = new List<int>();
                        foreach (int key in ActiveSecurities.Keys)
                        {
                            Security sec = ActiveSecurities[key];

                            if (sec.Active)
                                RunPublishSecurity(sec);
                            else
                                keysToRemove.Add(key);
                        }

                        keysToRemove.ForEach(x => ActiveSecurities.Remove(x));
                        keysToRemove.Clear();

                        foreach (int key in ActiveSecuritiesOnDemand.Keys)
                        {
                            Security sec = ActiveSecuritiesOnDemand[key];

                            if (sec.Active)
                                RunPublishSecurity(sec);
                            else
                                keysToRemove.Add(key);

                        }
                        keysToRemove.ForEach(x => ActiveSecuritiesOnDemand.Remove(x));
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{1}:There was an error publishing securities market data from market data flow error={0} ", ex.Message, IBConfiguration.Name),
                             Constants.MessageType.Error);

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
                                keysToRemove.Add(key);
                        }

                        foreach (int keyToRemove in keysToRemove)
                        {
                            ContractsTimeStamps.Remove(keyToRemove);

                            if (ActiveSecurities.ContainsKey(keyToRemove))
                            {
                                CancelMarketData(ActiveSecurities[keyToRemove]);
                                ActiveSecurities.Remove(keyToRemove);
                            }

                            if (ActiveSecuritiesOnDemand.ContainsKey(keyToRemove))
                            {
                                CancelMarketData(ActiveSecuritiesOnDemand[keyToRemove]);
                                ActiveSecuritiesOnDemand.Remove(keyToRemove);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("There was an error cleaning old securities from market data flow error={0} ", ex.Message), 
                              Constants.MessageType.Error);
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
                        zHFT.MarketClient.IB.Common.Converters.SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    if (ActiveSecuritiesOnDemand.ContainsKey(tickerId))
                    {

                        Security sec = ActiveSecuritiesOnDemand[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        zHFT.MarketClient.IB.Common.Converters.SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Constants.MessageType.Debug);

                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Constants.MessageType.Error);

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
                        zHFT.MarketClient.IB.Common.Converters.SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    if (ActiveSecuritiesOnDemand.ContainsKey(tickerId))
                    {
                        Security sec = ActiveSecuritiesOnDemand[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        zHFT.MarketClient.IB.Common.Converters.SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Constants.MessageType.Debug);

                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Constants.MessageType.Error);

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
                        zHFT.MarketClient.IB.Common.Converters.SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    if (ActiveSecuritiesOnDemand.ContainsKey(tickerId))
                    {
                        Security sec = ActiveSecuritiesOnDemand[tickerId];
                        sec.MarketData.MDLocalEntryDate = DateTime.Now;
                        zHFT.MarketClient.IB.Common.Converters.SecurityConverter.AssignValueBasedOnField(sec, field, value);

                    }
                    else
                        DoLog(string.Format("Unknown tickerId event: event={0} tickerId={1} field={2} ",
                                ev, tickerId, field), Constants.MessageType.Debug);

                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing event: event={0} tickerId={1} field={2} error={3} ",
                    ev, tickerId, field, ex.Message), Constants.MessageType.Error);
            }

        }
        
        protected void RequestOrderBookOnDemand(Security sec,bool snapshot,string mode)
        {
            zHFT.MarketClient.IB.Common.Configuration.Contract ctr = new zHFT.MarketClient.IB.Common.Configuration.Contract();

            ctr.Currency = SecurityConverter.GetCurrency(sec.SecType,sec.Currency,sec.Symbol,_SECURITY_SYMBOL_SEP_ORIG);
            //ctr.Exchange = sec.Exchange != null ? sec.Exchange : IBConfiguration.Exchange ;
            ctr.Exchange = sec.Exchange != null ? sec.Exchange : IBConfiguration.Exchange ;
            ctr.SecType = zHFT.InstructionBasedMarketClient.IB.Common.Converters.SecurityConverter.GetSecurityType(sec.SecType);
            ctr.Symbol = SecurityConverter.GetSymbol(sec.SecType, sec.Symbol, _SECURITY_SYMBOL_SEP_ORIG);
            ctr.PrimaryExchange = SecurityConverter.GetPrimaryExchange(sec.SecType);
            //sec.Symbol= sec.Symbol.Replace(_SECURITY_SYMBOL_SEP_ORIG,_SECURITY_SYMBOL_SEP_DEST);

            if (!ActiveSecurities.Values.Any(x => x.Symbol == sec.Symbol) 
                && !ActiveSecuritiesOnDemand.Values.Any(x => x.Symbol == sec.Symbol))
            {
                DoLog(string.Format("@{0}:Requesting {2} Order Book On Demand for Symbol: {1}", IBConfiguration.Name, sec.Symbol,mode), Constants.MessageType.Information);
                int tickerId = _MARKET_DATA_ON_DEMAND_INDEX + ActiveSecuritiesOnDemand.Count;
                ActiveSecuritiesOnDemand.Add(tickerId, sec);
                ContractsTimeStamps.Add(tickerId, DateTime.Now);
                ReqMarketDepth(tickerId, ctr);
            }
            else
                DoLog(string.Format("@{0}:Order Book already subscribed for symbol: {1}", IBConfiguration.Name, sec.Symbol), Constants.MessageType.Information);
        }

        protected void RequestMarketDataOnDemand(Security sec,bool snapshot,string mode)
        {
            zHFT.MarketClient.IB.Common.Configuration.Contract ctr = new zHFT.MarketClient.IB.Common.Configuration.Contract();
            
            ctr.Exchange = sec.Exchange != null ? sec.Exchange : IBConfiguration.Exchange ;
            ctr.SecType = zHFT.InstructionBasedMarketClient.IB.Common.Converters.SecurityConverter.GetSecurityType(sec.SecType);
            ctr.Currency = SecurityConverter.GetCurrency(sec.SecType,sec.Currency,sec.Symbol,_SECURITY_SYMBOL_SEP_ORIG);
            ctr.Symbol = SecurityConverter.GetSymbol(sec.SecType, sec.Symbol, _SECURITY_SYMBOL_SEP_ORIG);
            ctr.PrimaryExchange = SecurityConverter.GetPrimaryExchange(sec.SecType);
            //sec.Symbol= sec.Symbol.Replace(_SECURITY_SYMBOL_SEP_ORIG,_SECURITY_SYMBOL_SEP_DEST);

            if (!ActiveSecurities.Values.Any(x => x.Symbol == sec.Symbol) 
                && !ActiveSecuritiesOnDemand.Values.Any(x => x.Symbol == sec.Symbol))
            {
                DoLog(string.Format("@{0}:Requesting {2} Market Data On Demand for Symbol: {1}", IBConfiguration.Name, sec.Symbol,mode), Constants.MessageType.Information);
                int tickerId = _MARKET_DATA_ON_DEMAND_INDEX + ActiveSecuritiesOnDemand.Count();
                ActiveSecuritiesOnDemand.Add(tickerId, sec);
                ContractsTimeStamps.Add(tickerId, DateTime.Now);
                ReqMktData(tickerId, snapshot, ctr);
            }
            else
                DoLog(string.Format("@{0}:Market data already subscribed for symbol: {1}", IBConfiguration.Name, sec.Symbol), Constants.MessageType.Information);
        }

        protected void CancelMarketData(Security sec)
        { 
            if(ActiveSecurities.Values.Any(x=>x.Symbol==sec.Symbol))
            {
                int tickerId = ActiveSecurities.Where(x => x.Value.Symbol == sec.Symbol).FirstOrDefault().Key;
                ActiveSecurities[tickerId].Active = false;
                DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data On Demand for Symbol: {0}", IBConfiguration.Name, sec.Symbol), Constants.MessageType.Information);
                ClientSocket.cancelMktData(tickerId);
            }
            else if (ActiveSecuritiesOnDemand.Values.Any(x => x.Symbol == sec.Symbol))
            {
                int tickerId = ActiveSecuritiesOnDemand.Where(x => x.Value.Symbol == sec.Symbol).FirstOrDefault().Key;
                ActiveSecurities[tickerId].Active = false;
                DoLog(string.Format("@{0}:Requesting Unsubscribe Market Data On Demand for Symbol: {0}", IBConfiguration.Name, sec.Symbol), Constants.MessageType.Information);
                ClientSocket.cancelMktData(tickerId);
                
            }
            else
                throw new Exception(string.Format("@{0}: Could not find active security to unsubscribe for symbol {1}", IBConfiguration.Name, sec.Symbol));
         
        }

        protected CMState ProessMarketDataRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot not implemented for symbol {1}", IBConfiguration.Name,  mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    if (mdr.MarketDepth == null || mdr.MarketDepth == MarketDepth.TopOfBook)
                    {
                        RequestMarketDataOnDemand(mdr.Security, false, "Snapshot+Updates");
                        return CMState.BuildSuccess();
                    }
                    else if (mdr.MarketDepth == MarketDepth.FullBook)
                    {
                        RequestOrderBookOnDemand(mdr.Security, false, "Snapshot+Updates");
                        return CMState.BuildSuccess();
                    }
                    else
                    {
                        return CMState.BuildFail(new Exception(string.Format("Not implemented market depth {0} on order book request",mdr.MarketDepth)));
                    }
                    
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketData(mdr.Security);
                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", IBConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            { 
                return CMState.BuildFail(ex);
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
                    ActiveSecuritiesOnDemand = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();

                    ClientSocket = new EClientSocket(this, this);

                    ClientSocket.eConnect(IBConfiguration.IP, IBConfiguration.Port, IBConfiguration.IdIBClient);
                    
                    EReader= new EReader(ClientSocket, this);
                    EReader.Start();
                    
                    ReaderThread = new Thread(ReaderThreadImp){IsBackground = true};
                    
                    ReaderThread.Start();

                    PublishThread = new Thread(DoPublish);
                    PublishThread.Start();

                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + moduleConfigFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + moduleConfigFile + ":" + ex.Message, Constants.MessageType.Error);
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
                    if (Actions.MARKET_DATA_REQUEST == action)
                    {
                        return ProessMarketDataRequest(wrapper);
                    }
                    else
                    {
                        DoLog(string.Format("@{0}:Sending message {1} not implemented",IBConfiguration.Name,action.ToString()), Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message {1} not implemented", IBConfiguration.Name, action.ToString())));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");


            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Constants.MessageType.Error);
                throw;
            }
        }

        #endregion
    }
}

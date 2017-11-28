using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.MarketClient.IB.Common;
using zHFT.MarketClient.IB.Common.Converters;
using zHFT.MarketClient.IB.Common.Interfaces;


namespace zHFT.MarketClient.IB.Client
{
    public class IBMarketClient : IBMarketClientBase
    {
        #region Private Attributes
        
        public IConfiguration Config { get; set; }

        protected Thread PublishThread { get; set; }

        protected Thread DebugThread { get; set; }

        protected object tLock = new object();

        #endregion

        #region Constructors

        public IBMarketClient() { }

        #endregion

        #region Protected Methods

        protected override IConfiguration GetConfig() { return Config; }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new IB.Common.Configuration.Configuration().GetConfiguration<IB.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion

        #region Private Methods

        protected void DoDebugMarketData()
        {
            IB.Common.Configuration.Configuration ibConfig = (IB.Common.Configuration.Configuration)Config;

            while (true)
            {
                Thread.Sleep(ibConfig.MarketDataDebugRefresh);

                lock (tLock)
                {

                    foreach (Security sec in ContractRequests.Values)
                    {
                        try
                        {
                            if(!sec.MarketData.OpeningPrice.HasValue)
                                DoLog(string.Format("Market Data Warning! {0} has no opening price!", sec.Symbol), Main.Common.Util.Constants.MessageType.Debug);

                            if (!sec.MarketData.ClosingPrice.HasValue)
                                DoLog(string.Format("Market Data Warning! {0} has no closing price!", sec.Symbol), Main.Common.Util.Constants.MessageType.Debug);


                            if (!sec.MarketData.TradingSessionHighPrice.HasValue)
                                DoLog(string.Format("Market Data Warning! {0} has no high price!", sec.Symbol), Main.Common.Util.Constants.MessageType.Debug);


                            if (!sec.MarketData.TradingSessionLowPrice.HasValue)
                                DoLog(string.Format("Market Data Warning! {0} has no low price!", sec.Symbol), Main.Common.Util.Constants.MessageType.Debug);


                            if (!sec.MarketData.Trade.HasValue)
                                DoLog(string.Format("Market Data Warning! {0} has no last price!", sec.Symbol), Main.Common.Util.Constants.MessageType.Debug);

                            if (!sec.MarketData.TradeVolume.HasValue)
                                DoLog(string.Format("Market Data Warning! {0} has no volume!", sec.Symbol), Main.Common.Util.Constants.MessageType.Debug);


                        }
                        catch (Exception ex)
                        {
                            DoLog(string.Format("Error Publishing Market Data Warnings for Security {0}. Error={1} ",
                                                        sec.Symbol, ex != null ? ex.Message : ""),
                                                        Main.Common.Util.Constants.MessageType.Error);
                        }
                    }
                }
            }
        }

        protected void DoPublish()
        {
            IB.Common.Configuration.Configuration ibConfig = (IB.Common.Configuration.Configuration)Config;
            while(true)
            {
                Thread.Sleep(ibConfig.PublishUpdateInMilliseconds);

                lock (tLock)
                {
                    foreach (Security sec in ContractRequests.Values)
                    {
                        RunPublishSecurity(sec);
                    }
                }
            
            }
        }

        protected override void ProcessField(string ev,int tickerId, int field, double value)
        {
            try
            {
                lock (tLock)
                {
                    if (ContractRequests.ContainsKey(tickerId))
                    {
                        Security sec = ContractRequests[tickerId];
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
                    if (ContractRequests.ContainsKey(tickerId))
                    {
                        Security sec = ContractRequests[tickerId];
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
                    if (ContractRequests.ContainsKey(tickerId))
                    {
                        Security sec = ContractRequests[tickerId];
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

        protected void RunMarketDataSubscriptions(IList<zHFT.MarketClient.IB.Common.Configuration.Contract> contracts)
        {
            int i = 0;
            lock (tLock)
            {
                foreach (zHFT.MarketClient.IB.Common.Configuration.Contract ctr in contracts)
                {
                    try
                    {
                        DoLog(string.Format("Subscribing Market Data for symbol {0}", ctr.Symbol), Main.Common.Util.Constants.MessageType.Information);
                        ReqMktData(i,false, ctr);
                        //ReqMarketDepth(i, ctr);
                        ContractRequests.Add(i, BuildSecurityFromConfig(ctr));
                        i++;
                    }
                    catch (Exception ex)
                    {
                        DoLog("Error Requesting Market Data for security " + ctr.Symbol + ": " + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                    }
                }
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

                    ContractRequests = new Dictionary<int, Security>();
                    IB.Common.Configuration.Configuration ibConfig = (IB.Common.Configuration.Configuration)Config;

                    ClientSocket=new EClientSocket(this);
                    ClientSocket.eConnect(ibConfig.IP, ibConfig.Port, ibConfig.IdIBClient);

                    if (!string.IsNullOrEmpty(ibConfig.StockListAccessLayer))
                    {
                        //Implementar módulo de conexión por BD
                        var typeStockListAccessLayer = Type.GetType(ibConfig.StockListAccessLayer);
                        if (typeStockListAccessLayer != null)
                        {
                            IContractAccessLayer ibAccessLayer = (IContractAccessLayer)Activator.CreateInstance(typeStockListAccessLayer);

                            if (ibAccessLayer != null)
                            {
                                IList<zHFT.MarketClient.IB.Common.Configuration.Contract> contracts = ibAccessLayer.GetContracts((zHFT.MarketClient.IB.Common.Configuration.Configuration)Config, null);

                                if (contracts != null)
                                    RunMarketDataSubscriptions(contracts);
                                else
                                    DoLog("Error retrieving contracts from stock list access layer!!", Main.Common.Util.Constants.MessageType.Error);
                            }
                            else
                                DoLog("Error initializing stock list access layer!!", Main.Common.Util.Constants.MessageType.Error);
                            
                        }
                        else
                            throw new Exception("assembly not found: " + ibConfig.StockListAccessLayer);
                    }
                    else
                    {
                        RunMarketDataSubscriptions(ibConfig.ContractList);
                    }

                    PublishThread = new Thread(DoPublish);
                    PublishThread.Start();

                    DebugThread = new Thread(DoDebugMarketData);
                    DebugThread.Start();

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
                    DoLog("Sending message " + action+" not implemented", Main.Common.Util.Constants.MessageType.Information);


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

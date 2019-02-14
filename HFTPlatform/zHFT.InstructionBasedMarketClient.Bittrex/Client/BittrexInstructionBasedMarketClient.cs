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
using zHFT.InstructionBasedMarketClient.Bittrex.Common.Wrappers;
using zHFT.InstructionBasedMarketClient.Bittrex.DataAccessLayer.Managers;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Cryptos.Client;
using zHFT.InstructionBasedMarketClient.Cryptos.DataAccessLayer.Managers;
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
    public class BittrexInstructionBasedMarketClient : BaseInstructionBasedMarketClient
    {
        #region Protected Attributes

        public Exchange Exchange { get; set; }

        public ExchangeContext ExchangeContext { get; set; }

        protected Bittrex.Common.Configuration.Configuration BittrexConfiguration
        {
            get { return (Bittrex.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private Dictionary<string, bool> ReverseCurrency { get; set; }

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

        private void ReverseRequestMarketData(string symbol)
        {
            Exchange exch = new Exchange();
            ExchangeContext ctx = GetContext();
            ctx.QuoteCurrency = symbol;
            exch.Initialise(ctx);

            JObject jMarketData = exch.GetTicker(BittrexConfiguration.QuoteCurrency);

            Security sec = new Security();
            sec.Symbol = symbol;
            sec.MarketData.BestBidPrice = (double?)jMarketData["Bid"];
            sec.MarketData.BestAskPrice =  (double?)jMarketData["Ask"];
            sec.MarketData.Trade = (double?)jMarketData["Last"];
            sec.ReverseMarketData = true;
            BittrexMarketDataWrapper wrapper = new BittrexMarketDataWrapper(sec, BittrexConfiguration);

            OnMessageRcv(wrapper);
        }

        protected override void DoRequestMarketData(Object param)
        {
            string symbol = (string)param;
            try
            {
                DoLog(string.Format("@{0}:Requesting market data por symbol {1}", BittrexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);

                bool activo = true;
                while (activo)
                {
                    Thread.Sleep(BittrexConfiguration.PublishUpdateInMilliseconds);

                    lock (tLock)
                    {
                        if (ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                        {
                            try
                            {
                                Exchange exch = new Exchange();
                                ExchangeContext ctx = GetContext();
                                exch.Initialise(ctx);

                                //Probamos la versión derecha del mercado
                                try
                                {
                                    if (!ReverseCurrency.Keys.Contains(symbol))
                                    {
                                        JObject jMarketData = exch.GetTicker(symbol);

                                        Security sec = new Security();
                                        sec.Symbol = symbol;
                                        sec.MarketData.BestBidPrice = (double?)jMarketData["Bid"];
                                        sec.MarketData.BestAskPrice = (double?)jMarketData["Ask"];
                                        //sec.MarketData.Trade = (double?)jMarketData["Last"];
                                        sec.ReverseMarketData = false;


                                        GetMarketHistoryResponse resp = exch.GetMarketHistory(symbol);

                                        MarketTrade trade = resp.OrderByDescending(x => x.TimeStamp).FirstOrDefault();
                                        sec.MarketData.Trade = Convert.ToDouble(trade.Price);
                                        sec.MarketData.MDTradeSize = Convert.ToDouble(trade.Quantity);
                                    
                                        sec.MarketData.LastTradeDateTime = trade.TimeStamp;

                                        BittrexMarketDataWrapper wrapper = new BittrexMarketDataWrapper(sec, BittrexConfiguration);

                                        OnMessageRcv(wrapper);
                                    }
                                    else
                                        ReverseRequestMarketData(symbol);
                                }
                                catch (Exception ex)
                                {
                                    if (!ReverseCurrency.Keys.Contains(symbol))
                                        ReverseCurrency.Add(symbol, true);
                                    ReverseRequestMarketData(symbol);
                                }
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("@{0}:Error Requesting market data for symbol {1}:{2}",
                                        BittrexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Information);
                                activo = false;
                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}:Unsubscribing market data for symbol {1}", BittrexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);

                            activo = false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                lock (tLock)
                {
                    RemoveSymbol(symbol);
                }

                DoLog(string.Format("@{0}: Error Requesting market data por symbol {1}:{2}", BittrexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        
        }

        protected override int GetSearchForInstrInMiliseconds()
        {
            return BittrexConfiguration.SearchForInstructionsInMilliseconds;
        }

        protected override BaseConfiguration GetConfig()
        {
            return BittrexConfiguration;
        }

        protected override int GetAccountNumber()
        {
            return BittrexConfiguration.AccountNumber;
        }

        protected override CMState ProessMarketDataRequest(Wrapper wrapper)
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
                    return ProcessMarketDataRequest(wrapper);
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

                    CleanPrevInstructions();

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

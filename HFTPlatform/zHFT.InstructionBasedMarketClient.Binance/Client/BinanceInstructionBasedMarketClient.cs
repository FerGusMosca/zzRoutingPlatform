using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Market;
using zHFT.InstructionBasedMarketClient.Binance.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Binance.Common.Util;
using zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers;
using zHFT.InstructionBasedMarketClient.Binance.DataAccessLayer.Managers;
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

namespace zHFT.InstructionBasedMarketClient.Binance.Client
{
    public class BinanceInstructionBasedMarketClient : BaseInstructionBasedMarketClient
    {
        #region Private  Consts

        private int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        private int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        #endregion

        #region Protected Attributes

        protected Binance.Common.Configuration.Configuration BinanceConfiguration
        {
            get { return (Binance.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private Dictionary<string, bool> ReverseCurrency { get; set; }

        private AccountBinanceDataManager AccountBinanceDataManager { get; set; }
        
        protected AccountBinanceData AccountBinanceData { get; set; }

        #endregion

        #region Protected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Binance.Common.Configuration.Configuration().GetConfiguration<Binance.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected override void DoRequestMarketData(Object param)
        { 
           string symbol = (string)((object[])param)[0];
           string quoteSymbol = (string)((object[])param)[1];
           CultureInfo prevCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                DoLog(string.Format("@{0}:Requesting market data por symbol {1}{2}", BinanceConfiguration.Name, symbol,quoteSymbol), Main.Common.Util.Constants.MessageType.Information);

                var apiClient = new ApiClient(AccountBinanceData.APIKey,AccountBinanceData.Secret);
                var binanceClient = new BinanceClient(apiClient);
                
                bool activo = true;
                while (activo)
                {
                    Thread.Sleep(BinanceConfiguration.PublishUpdateInMilliseconds);
                    CultureInfo tempCulture = new CultureInfo("ja-JP");

                    if (quoteSymbol == null)
                        quoteSymbol = BinanceConfiguration.QuoteCurrency;
                    
                    lock (tLock)
                    {
                        if (ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol ==symbol))
                        {
                            OrderBook orderBook = binanceClient.GetOrderBook(symbol + quoteSymbol).Result;
                            List<PriceChangeInfo> priceChangeInfos = new List<PriceChangeInfo>(binanceClient.GetPriceChange24H(symbol + quoteSymbol).Result);
                            
                            List<OrderBookOffer> bids =new List<OrderBookOffer>(orderBook.Bids);
                            List<OrderBookOffer> asks =new List<OrderBookOffer>(orderBook.Asks);

                            Security sec = new Security();
                            sec.Symbol = symbol;
                            sec.MarketData.BestBidPrice =bids.Count() > 0 ? (double?) Convert.ToDouble(bids[0].Price) : null;
                            sec.MarketData.BestBidCashSize = bids.Count() > 0 ? (decimal?) Convert.ToDecimal(bids[0].Quantity) : null;
                            sec.MarketData.BestAskPrice = asks.Count() > 0 ? (double?) Convert.ToDouble(asks[0].Price) : null;
                            sec.MarketData.BestAskCashSize = asks.Count() > 0 ? (decimal?) Convert.ToDecimal(asks[0].Quantity) : null;
                            //sec.MarketData.Trade = priceChange.Count() > 0? (double?) Convert.ToDouble(priceChange[0].LastPrice): null;
                            sec.MarketData.Trade = priceChangeInfos!=null&&priceChangeInfos.Count>0?(double?)priceChangeInfos.FirstOrDefault().LastPrice:null;
                            sec.ReverseMarketData = false;

                            BinanceMarketDataWrapper wrapper = new BinanceMarketDataWrapper(sec, BinanceConfiguration);
                            
                            OnMessageRcv(wrapper);
                        }
                        else
                        {
                            DoLog(string.Format("@{0}:Unsubscribing market data por symbol {1}", BinanceConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
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

                DoLog(string.Format("@{0}: Error Requesting market data por symbol {1}:{2}", BinanceConfiguration.Name, symbol, BinanceErrorFormatter.ProcessErrorMessage(ex)), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected override CMState ProessMarketDataRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    return ProcessMarketDataRequest(wrapper);
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
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", GetConfig().Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected override int GetSearchForInstrInMiliseconds()
        {
            throw new NotImplementedException();
        }

        protected override BaseConfiguration GetConfig()
        {
            return BinanceConfiguration;
        }

        protected override int GetAccountNumber()
        {
            throw new NotImplementedException();
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

                    ActiveSecurities = new Dictionary<long, Security>();
                    ContractsTimeStamps = new Dictionary<long, DateTime>();
                    ReverseCurrency = new Dictionary<string, bool>();

                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    AccountBinanceDataManager = new AccountBinanceDataManager(BinanceConfiguration.EFConnectionString);

                    AccountBinanceData= AccountBinanceDataManager.GetByAccountNumber(new Account(){AccountNumber = BinanceConfiguration.AccountNumber});

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
                DoLog("Critical error initializing " + configFile + ":" + BinanceErrorFormatter.ProcessErrorMessage(ex), Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}

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
using OrderBook = Binance.API.Csharp.Client.Models.Market.OrderBook;

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

        protected void BuildBinanceData()
        {
            if (BinanceConfiguration.AccountNumber.HasValue)
            {
                AccountBinanceData = AccountBinanceDataManager.GetByAccountNumber(new Account(){AccountNumber = BinanceConfiguration.AccountNumber.Value});
            }
            else if(!string.IsNullOrEmpty(BinanceConfiguration.Secret) && !string.IsNullOrEmpty(BinanceConfiguration.Key))
            {
                AccountBinanceData= new AccountBinanceData(){Secret = BinanceConfiguration.Secret,APIKey = BinanceConfiguration.Key};
            }
            else
                throw new Exception(String.Format("Could not find biannce keys. Not an account number or secret/key pair detected"));
            
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Binance.Common.Configuration.Configuration().GetConfiguration<Binance.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected Security DoPopulateL1(string symbol,string quoteSymbol, List<OrderBookOffer> bids,List<OrderBookOffer> asks)
        {
            var apiClient = new ApiClient(AccountBinanceData.APIKey,AccountBinanceData.Secret);
            var binanceClient = new BinanceClient(apiClient);
            
            List<PriceChangeInfo> priceChangeInfos = new List<PriceChangeInfo>(binanceClient.GetPriceChange24H(symbol + quoteSymbol).Result);

            Security sec = new Security();
            sec.Symbol = symbol;
            sec.MarketData.BestBidPrice =bids.Count() > 0 ? (double?) Convert.ToDouble(bids[0].Price) : null;
            sec.MarketData.BestBidCashSize = bids.Count() > 0 ? (decimal?) Convert.ToDecimal(bids[0].Quantity) : null;
            sec.MarketData.BestAskPrice = asks.Count() > 0 ? (double?) Convert.ToDouble(asks[0].Price) : null;
            sec.MarketData.BestAskCashSize = asks.Count() > 0 ? (decimal?) Convert.ToDecimal(asks[0].Quantity) : null;
            //sec.MarketData.Trade = priceChange.Count() > 0? (double?) Convert.ToDouble(priceChange[0].LastPrice): null;
            sec.MarketData.Trade = priceChangeInfos!=null&&priceChangeInfos.Count>0?(double?)priceChangeInfos.FirstOrDefault().LastPrice:null;
            sec.ReverseMarketData = false;

            return sec;

        }

        protected override void DoRequestOrderBook(Object param)
        { 
            
            string symbol = (string)((object[])param)[0];
            string quoteSymbol = (string)((object[])param)[1];
            CultureInfo prevCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                DoLog(string.Format("@{0}:Requesting order book for symbol {1}{2}", BinanceConfiguration.Name, symbol,quoteSymbol), Main.Common.Util.Constants.MessageType.Information);

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
                        if (!ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                        {
                            DoLog(string.Format("@{0}:Unsubscribing order book por symbol {1}", BinanceConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                            activo = false;
                            continue;
                        }
                            
                    }
                    
                    OrderBook orderBook = binanceClient.GetOrderBook(symbol + quoteSymbol).Result;
                    List<OrderBookOffer> bids =new List<OrderBookOffer>(orderBook.Bids);
                    List<OrderBookOffer> asks =new List<OrderBookOffer>(orderBook.Asks);

                    Security sec = DoPopulateL1(symbol, quoteSymbol, bids, asks);

                    BinanceMarketDataWrapper wrapper = new BinanceMarketDataWrapper(sec,bids,asks, BinanceConfiguration);
                            
                    OnMessageRcv(wrapper);
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
                        if (!ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                        {
                            DoLog(string.Format("@{0}:Unsubscribing market data por symbol {1}", BinanceConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                            activo = false;
                            continue;
                        }
                            
                    }
                    
                    List<PriceChangeInfo> priceChangeInfos = new List<PriceChangeInfo>(binanceClient.GetPriceChange24H(symbol + quoteSymbol).Result);
                        
                    PriceChangeInfo priceChg=priceChangeInfos.Count>0? priceChangeInfos.OrderByDescending(x=>x.LastId).FirstOrDefault():null;
                        
                    Security sec =  new Security();;
                    sec.Symbol = symbol;
                    sec.MarketData.BestBidPrice = priceChg != null ? (double?) priceChg.BidPrice : null;
                    sec.MarketData.BestAskPrice=priceChg != null ? (double?) priceChg.AskPrice : null;
                    sec.MarketData.Trade = priceChg != null ? (double?) priceChg.LastPrice : null;
                    sec.ReverseMarketData = false;
                    sec.MarketData.MDEntryDate = priceChg != null ? (DateTime?) DateTime.Now : null;
                    sec.MarketData.OpeningPrice = priceChg != null ? (double?) priceChg.OpenPrice : null;
                    sec.MarketData.ClosingPrice = priceChg != null ? (double?) priceChg.LastPrice : null;
                    sec.MarketData.TradingSessionHighPrice = priceChg != null ? (double?) priceChg.HighPrice : null;
                    sec.MarketData.TradingSessionLowPrice = priceChg != null ? (double?) priceChg.LowPrice : null;
                    sec.MarketData.CashVolume = priceChg != null ? (double?) priceChg.Volume : null;

                    BinanceMarketDataWrapper wrapper = new BinanceMarketDataWrapper(sec, BinanceConfiguration);
                        
                    OnMessageRcv(wrapper);
                    
                }
            }
            catch (Exception ex)
            {
                lock (tLock)
                {
                    RemoveSymbol(symbol);
                }

                DoLog(string.Format("@{0}: ERRPR- Requesting market data por symbol {1}:{2}", BinanceConfiguration.Name, symbol, BinanceErrorFormatter.ProcessErrorMessage(ex)), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected override CMState ProcessMarketDataRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    return OnMarketDataRequest(wrapper);
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    
                    if (mdr.MarketDepth == null || mdr.MarketDepth == MarketDepth.TopOfBook)
                    {
                        OnMarketDataRequest(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (mdr.MarketDepth == MarketDepth.FullBook)
                    {
                        OnOrderBookRequest(wrapper);
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
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", GetConfig().Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Processing market data request :{1}", BinanceConfiguration.Name,  BinanceErrorFormatter.ProcessErrorMessage(ex)), Main.Common.Util.Constants.MessageType.Error);

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

                    
                    if(BinanceConfiguration.EFConnectionString!=null)
                        AccountBinanceDataManager = new AccountBinanceDataManager(BinanceConfiguration.EFConnectionString);

                    BuildBinanceData();
                    
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

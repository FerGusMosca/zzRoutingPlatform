using Binance.Net.Objects.Models.Spot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.Binance.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Binance.DataAccessLayer.Managers.ADO;
using zHFT.InstructionBasedMarketClient.Cryptos.Client;
using zHFT.Main.Common.Wrappers;

using BinanceNet = Binance.Net;
using BinanceClientOptions = Binance.Net.Objects.Options.BinanceOptions;
using BinanceApiCredentials = CryptoExchange.Net.Authentication.ApiCredentials;
using zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.InstructionBasedMarketClient.Binance.Common.Util;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using static zHFT.Main.Common.Util.Constants;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.MarketClient.Common.Converters;
using zHFT.InstructionBasedMarketClient.Binance.Common.Configuration;
using Binance.Net.Objects.Models;
using Binance.API.Csharp.Client.Models.Market;
using zHFT.MarketClient.Common.Common.Wrappers;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.Net.Enums;

namespace tph.InstructionBasedMarketClient.Binance2.Client
{
    public class BinanceInstructionBasedMarketClient : BaseInstructionBasedMarketClient
    {

        #region Private  Consts

        private int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        private int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        #endregion

        #region Protected Attributes

        protected zHFT.InstructionBasedMarketClient.Binance.Common.Configuration.Configuration BinanceConfiguration
        {
            get { return (zHFT.InstructionBasedMarketClient.Binance.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private Dictionary<string, bool> ReverseCurrency { get; set; }

        protected Dictionary<int, Wrapper> HistoricalPricesRequests { get; set; }

        //private AccountBinanceDataManager AccountBinanceDataManager { get; set; }

        private AccountBinanceDataManager AccountBinanceDataManager { get; set; }

        protected AccountBinanceData AccountBinanceData { get; set; }

        #endregion

        #region Protected Methods

        protected void BuildBinanceData()
        {
            if (BinanceConfiguration.AccountNumber.HasValue)
            {
                AccountBinanceData = AccountBinanceDataManager.GetAccountBinanceData(BinanceConfiguration.AccountNumber.Value);
            }
            else if (!string.IsNullOrEmpty(BinanceConfiguration.Secret) && !string.IsNullOrEmpty(BinanceConfiguration.Key))
            {
                AccountBinanceData = new AccountBinanceData() { Secret = BinanceConfiguration.Secret, APIKey = BinanceConfiguration.Key };
            }
            else
                throw new Exception(String.Format("Could not find biannce keys. Not an account number or secret/key pair detected"));

        }

        protected async void  DoPopulateL1(Security sec, string symbol, string quoteSymbol)
        {
            var apiCredentials = new BinanceApiCredentials(AccountBinanceData.APIKey, AccountBinanceData.Secret);

            // Crea el cliente de Binance
            var binanceClient = new Binance.Net.Clients.BinanceRestClient(
                   options =>
                   {
                       options.ApiCredentials = apiCredentials;
                   });

            var orderBook = await binanceClient.SpotApi.ExchangeData.GetOrderBookAsync(symbol + quoteSymbol);

            if (orderBook.Data != null)
            {
                IEnumerable<BinanceOrderBookEntry> bids = orderBook.Data.Bids;
                IEnumerable<BinanceOrderBookEntry> asks = orderBook.Data.Asks;


                BinanceOrderBookEntry bestBid = bids.OrderByDescending(x => x.Price).FirstOrDefault();
                BinanceOrderBookEntry bestAsk = asks.OrderBy(x => x.Price).FirstOrDefault();

                sec.MarketData.BestBidPrice = bestBid != null ? (double?)Convert.ToDouble(bestBid.Price) : null;
                sec.MarketData.BestBidCashSize = bestBid != null ? (decimal?)Convert.ToDecimal(bestBid.Quantity) : null;
                sec.MarketData.BestAskPrice = bestAsk != null ? (double?)Convert.ToDouble(bestAsk.Price) : null;
                sec.MarketData.BestAskCashSize = bestAsk != null ? (decimal?)Convert.ToDecimal(bestAsk.Quantity) : null;
            }
        }

        protected override async void DoRequestMarketData(Object param)
        {
            string symbol = (string)((object[])param)[0];
            string quoteSymbol = (string)((object[])param)[1];
            CultureInfo prevCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                DoLog(string.Format("@{0}: Requesting market data for symbol {1}{2}", BinanceConfiguration.Name, symbol, quoteSymbol),zHFT.Main.Common.Util.Constants.MessageType.Information);

                // Configura las credenciales de la API
                var apiCredentials = new BinanceApiCredentials(AccountBinanceData.APIKey, AccountBinanceData.Secret);

                // Crea el cliente de Binance
                var binanceClient = new Binance.Net.Clients.BinanceRestClient(
                       options =>
                       {
                           options.ApiCredentials = apiCredentials;
                       });

                bool active = true;
                while (active)
                {
                    try
                    {
                        Thread.Sleep(BinanceConfiguration.PublishUpdateInMilliseconds);
                        CultureInfo tempCulture = new CultureInfo("ja-JP");

                        if (quoteSymbol == null)
                            quoteSymbol = BinanceConfiguration.QuoteCurrency;

                        lock (tLock)
                        {
                            if (!ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                            {
                                DoLog(string.Format("@{0}: Unsubscribing market data for symbol {1}", BinanceConfiguration.Name, symbol), MessageType.Information);
                                active = false;
                                continue;
                            }
                        }

                        var tradingDayInfo = await binanceClient.SpotApi.ExchangeData.GetTradingDayTickerAsync(symbol + quoteSymbol);
                                            Security sec = new Security();

                        if (tradingDayInfo.Data != null)
                        {
                            sec.Symbol = symbol;
                            sec.MarketData.Trade = tradingDayInfo != null ? (double?)tradingDayInfo.Data.LastPrice : null;
                            sec.ReverseMarketData = false;
                            sec.MarketData.MDEntryDate = tradingDayInfo != null && tradingDayInfo.Data != null ? (DateTime?)DateTime.Now : null;
                            sec.MarketData.OpeningPrice = tradingDayInfo != null && tradingDayInfo.Data != null ? (double?)tradingDayInfo.Data.OpenPrice : null;
                            sec.MarketData.ClosingPrice = tradingDayInfo != null && tradingDayInfo.Data != null ? (double?)tradingDayInfo.Data.LastPrice : null;
                            sec.MarketData.TradingSessionHighPrice = tradingDayInfo != null && tradingDayInfo.Data != null ? (double?)tradingDayInfo.Data.HighPrice : null;
                            sec.MarketData.TradingSessionLowPrice = tradingDayInfo != null && tradingDayInfo.Data != null ? (double?)tradingDayInfo.Data.LowPrice : null;
                            sec.MarketData.CashVolume = tradingDayInfo != null && tradingDayInfo.Data != null ? (double?)tradingDayInfo.Data.Volume : null;

                            DoPopulateL1(sec, symbol, quoteSymbol);

                            BinanceMarketDataWrapper wrapper = new BinanceMarketDataWrapper(sec, BinanceConfiguration);
                            OnMessageRcv(wrapper);
                        }
                        else
                        {
                            DoLog($"Ignoring market data for symbol {symbol} as not Data was received", MessageType.Information);
                        }

                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{0}: ERROR- Requesting market data for symbol {1}:{2}", BinanceConfiguration.Name, symbol, BinanceErrorFormatter.ProcessErrorMessage(ex)), MessageType.Error);
                    }

                }
            }
            catch (Exception ex)
            {
                lock (tLock)
                {
                    RemoveSymbol(symbol);
                }

                DoLog(string.Format("@{0}: Critical ERROR- Requesting market data for symbol {1}:{2}", BinanceConfiguration.Name, symbol, BinanceErrorFormatter.ProcessErrorMessage(ex)), MessageType.Error);
            }
        }

        protected KlineInterval GetTimeInterval(zHFT.Main.Common.Enums.CandleInterval interval)
        {
            KlineInterval? binanceInterval = null;
            if (interval == CandleInterval.Minute_1)
                binanceInterval = KlineInterval.OneMinute;
            else if (interval == CandleInterval.HOUR_1)
                binanceInterval = KlineInterval.OneHour;
            else if (interval == CandleInterval.DAY)
                binanceInterval = KlineInterval.OneDay;
            else
            {
                throw new Exception(string.Format("TimeInterval {0} not implemented on Market Data Request", interval));
            }

            return binanceInterval.Value;
        }

        protected async void DoRequestHistoricalPricesRequest(object param)
        {

            Wrapper wrapper = (Wrapper)((object[])param)[0];
            try
            {
                var apiCredentials = new BinanceApiCredentials(AccountBinanceData.APIKey, AccountBinanceData.Secret);

                // Crea el cliente de Binance
                var binanceClient = new Binance.Net.Clients.BinanceRestClient(
                       options =>
                       {
                           options.ApiCredentials = apiCredentials;
                       });

                string symbol = wrapper.GetField(HistoricalPricesRequestFields.Symbol) as string; //Symbol + quote symbol
                DateTime? from = wrapper.GetField(HistoricalPricesRequestFields.From) as DateTime?;
                DateTime? to = wrapper.GetField(HistoricalPricesRequestFields.To) as DateTime?;
                zHFT.Main.Common.Enums.CandleInterval interval = (zHFT.Main.Common.Enums.CandleInterval)wrapper.GetField(HistoricalPricesRequestFields.Interval);
                string quoteSymbol = BinanceConfiguration.QuoteCurrency;


                var histPrices = await binanceClient.SpotApi.ExchangeData.GetKlinesAsync(symbol + quoteSymbol, GetTimeInterval(interval),from,to);

                List<BinanceMarketDataWrapper> result = new List<BinanceMarketDataWrapper>();

                if (histPrices.Data != null)
                {
                    foreach (BinanceSpotKline candle  in histPrices.Data.OrderBy(x => x.OpenTime))
                {

                        Security sec = new Security();
                        ;
                        sec.Symbol = symbol;
                        sec.MarketData.Trade = (double?)candle.ClosePrice;
                        sec.ReverseMarketData = false;
                        sec.MarketData.MDEntryDate = candle.OpenTime;
                        sec.MarketData.OpeningPrice = (double?)candle.OpenPrice;
                        sec.MarketData.ClosingPrice = (double?)candle.ClosePrice;
                        sec.MarketData.TradingSessionHighPrice = (double?)candle.HighPrice;
                        sec.MarketData.TradingSessionLowPrice = (double?)candle.LowPrice;
                        sec.MarketData.CashVolume = (double?)candle.Volume;

                        BinanceMarketDataWrapper mdWrapper = new BinanceMarketDataWrapper(sec, BinanceConfiguration);

                        result.Add(mdWrapper);
                    }
                }

                Security mainSec = new Security();
                mainSec.Symbol = symbol;

                HistoricalPricesWrapper histWrapper = new HistoricalPricesWrapper(0, mainSec, CandleInterval.Minute_1, new List<Wrapper>(result));

                OnMessageRcv(histWrapper);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Processing historical candles request :{1}", BinanceConfiguration.Name, BinanceErrorFormatter.ProcessErrorMessage(ex)), MessageType.Error);
            }
        }

        protected override CMState ProcessHistoricalPricesRequest(Wrapper wrapper)
        {
            int reqId = (int)wrapper.GetField(HistoricalPricesRequestFields.MDReqId);

            if (!HistoricalPricesRequests.ContainsKey(reqId))
            {
                RequestMarketDataThread = new Thread(DoRequestHistoricalPricesRequest);
                RequestMarketDataThread.Start(new object[] { wrapper });
                HistoricalPricesRequests.Add(reqId, wrapper);
            }

            return CMState.BuildSuccess();
        }

        protected override void DoRequestOrderBook(object param)
        {
            string symbol = (string)((object[])param)[0];
            string quoteSymbol = (string)((object[])param)[1];
            CultureInfo prevCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                DoLog(string.Format("@{0}:Requesting order book for symbol {1}{2}", BinanceConfiguration.Name, symbol, quoteSymbol), MessageType.Information);
                bool active = true;
                while (active)
                {
                    try
                    {
                        Thread.Sleep(BinanceConfiguration.PublishUpdateInMilliseconds);
                        CultureInfo tempCulture = new CultureInfo("ja-JP");

                        if (quoteSymbol == null)
                            quoteSymbol = BinanceConfiguration.QuoteCurrency;

                        lock (tLock)
                        {
                            if (!ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                            {
                                DoLog(string.Format("@{0}:Unsubscribing order book por symbol {1}", BinanceConfiguration.Name, symbol), MessageType.Information);
                                active = false;
                                continue;
                            }

                        }

                        Security sec = new Security();
                        DoPopulateL1(null, symbol, quoteSymbol);

                        BinanceMarketDataWrapper wrapper = new BinanceMarketDataWrapper(sec,  BinanceConfiguration);
                        OnMessageRcv(wrapper);
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("@{0}: temp ERROR Requesting order book for symbol {1}:{2}", BinanceConfiguration.Name, symbol, BinanceErrorFormatter.ProcessErrorMessage(ex)), MessageType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (tLock)
                {
                    RemoveSymbol(symbol);
                }

                DoLog(string.Format("@{0}: Critical ERROR Requesting order book for symbol {1}:{2}", BinanceConfiguration.Name, symbol, BinanceErrorFormatter.ProcessErrorMessage(ex)), MessageType.Error);
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
                        return CMState.BuildFail(new Exception(string.Format("Not implemented market depth {0} on order book request", mdr.MarketDepth)));
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
                DoLog(string.Format("@{0}: Error Processing market data request :{1}", BinanceConfiguration.Name, BinanceErrorFormatter.ProcessErrorMessage(ex)), MessageType.Error);

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

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        #endregion

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
                    HistoricalPricesRequests = new Dictionary<int, Wrapper>();

                    CleanOldSecuritiesThread = new Thread(DoCleanOldSecurities);
                    CleanOldSecuritiesThread.Start();

                    if (BinanceConfiguration.ConnectionString != null)
                    {
                        AccountBinanceDataManager = new AccountBinanceDataManager(BinanceConfiguration.ConnectionString);
                    }
                    BuildBinanceData();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critical error initializing " + configFile + ":" + BinanceErrorFormatter.ProcessErrorMessage(ex), MessageType.Error);
                return false;
            }
        }

    }
}

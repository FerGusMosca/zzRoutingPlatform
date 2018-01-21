using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.Binance.BusinessEntities;
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

        #endregion

        #region Protected Methods

        protected void LoadAppCulture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }


        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Binance.Common.Configuration.Configuration().GetConfiguration<Binance.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected override void DoRequestMarketData(Object param)
        { 
           Instruction instrx = (Instruction)param;
           CultureInfo prevCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                DoLog(string.Format("@{0}:Requesting market data por symbol {1}", BinanceConfiguration.Name, instrx.Symbol), Main.Common.Util.Constants.MessageType.Information);

                bool activo = true;
                while (activo)
                {
                    Thread.Sleep(BinanceConfiguration.PublishUpdateInMilliseconds);
                    CultureInfo tempCulture = new CultureInfo("ja-JP");

                    lock (tLock)
                    {
                        LoadAppCulture(tempCulture);

                        var apiClient = new ApiClient(BinanceConfiguration.ApiKey, BinanceConfiguration.Secret);
                        var binanceClient = new BinanceClient(apiClient);
                        string fullSymbol = instrx.Symbol + BinanceConfiguration.QuoteCurrency;

                        var respOB = binanceClient.GetOrderBook(fullSymbol, 5);
                        OrderBook jOrderBook = respOB.Result;

                        OrderBookOffer bestSell = jOrderBook.Asks.OrderByDescending(x => x.Price).FirstOrDefault();
                        OrderBookOffer bestBuy = jOrderBook.Bids.OrderBy(x => x.Price).FirstOrDefault();

                        var respMD = binanceClient.GetCandleSticks(fullSymbol, TimeInterval.Minutes_1,null, null,1);
                        Candlestick jMarketData = respMD.Result.OrderByDescending(x=>x.CloseTime).FirstOrDefault();

                        Security sec = new Security();
                        sec.Symbol = instrx.Symbol;
                        sec.MarketData.BestBidPrice = Convert.ToDouble(bestBuy.Price);
                        //sec.MarketData.BestBidSize = Convert.ToInt64(bestBuy.Quantity);
                        sec.MarketData.BestAskPrice = Convert.ToDouble(bestSell.Price);
                        //sec.MarketData.BestAskSize = Convert.ToInt64(bestSell.Quantity);
                        sec.MarketData.Trade = Convert.ToDouble(jMarketData.Close);
                        sec.ReverseMarketData = false;

                        LoadAppCulture(prevCulture);

                        BinanceMarketDataWrapper wrapper = new BinanceMarketDataWrapper(sec, BinanceConfiguration);

                        OnMessageRcv(wrapper);
                    }
                }
            }
             catch (Exception ex)
            {
                lock (tLock)
                {
                    RemoveSymbol(instrx.Symbol);
                }

                DoLog(string.Format("@{0}: Error Requesting market data por symbol {1}:{2}", BinanceConfiguration.Name, instrx.Symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected override CMState ProessMarketDataRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot not implemented for symbol {1}", GetConfig().Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot+updates not implemented for symbol {1}", GetConfig().Name, mdr.Security.Symbol));
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
            return BinanceConfiguration.SearchForInstructionsInMilliseconds;
        }

        protected override BaseConfiguration GetConfig()
        {
            return BinanceConfiguration;
        }

        protected override int GetAccountNumber()
        {
            return BinanceConfiguration.AccountNumber;
        }

        protected void ConfigBinanceData()
        {
            Account account = AccountManager.GetByAccountNumber(BinanceConfiguration.AccountNumber);

            if (account == null)
                throw new Exception(string.Format("No se encontró ninguna cuenta para el número {0}", BinanceConfiguration.AccountNumber));

            AccountBinanceData binanceData = AccountBinanceDataManager.GetByAccountNumber(account);

            if (binanceData == null)
                throw new Exception(string.Format("No se encontró ninguna configuración bittrex para la cuenta número {0}", BinanceConfiguration.AccountNumber));


            BinanceConfiguration.ApiKey = binanceData.APIKey;
            BinanceConfiguration.Secret = binanceData.Secret;
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

                    AccountManager = new AccountManager(BinanceConfiguration.InstructionsAccessLayerConnectionString);
                    InstructionManager = new InstructionManager(BinanceConfiguration.InstructionsAccessLayerConnectionString, AccountManager);
                    AccountBinanceDataManager = new AccountBinanceDataManager(BinanceConfiguration.InstructionsAccessLayerConnectionString);

                    ConfigBinanceData();

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

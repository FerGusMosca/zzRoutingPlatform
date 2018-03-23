using Bittrex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.IBR.Bittrex.BusinessEntities;
using zHFT.StrategyHandler.IBR.Bittrex.DataAccessLayer.Managers;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.IBR.Bittrex.DataAccessLayer
{
    public class BittrexMarketDataManager : IMarketDataReferenceHandler
    {
        #region Private Consts

        private string _ACCOUNT_NUMBER = "AccountNumber";
        private string _CONFIG_CONNECTION_STRING = "ConfigConnectionString";
        private string _MARKET_DATA_QUOTE_CURRENCY = "MarketDataQuoteCurrency";
        private string _SIMULATE = "Simulate";

        private string _USD_CURRENCY = "USDT";
        private string _BTC_CURRENCY = "BTC";

        #endregion

        #region Protected Attributes

        protected List<ConfigKey> ConfigParameters { get; set; }

        protected AccountBittrexData BittrexData { get; set; }

        protected Exchange Exchange { get; set; }

        protected ExchangeContext ExchangeContext { get; set; }

        #endregion

        #region Constructors

        public BittrexMarketDataManager(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            ConfigParameters = pConfigParameters;
            ValidateDictionary();

            LoadConfig();

            Exchange = new Exchange();

            ExchangeContext = GetContext();

            Exchange.Initialise(ExchangeContext);
        
        }

        #endregion

        #region Protected Methods

        protected ExchangeContext GetContext()
        {
            return new ExchangeContext()
            {
                ApiKey = BittrexData.APIKey,
                QuoteCurrency = ConfigParameters.Where(x => x.Key == _MARKET_DATA_QUOTE_CURRENCY).FirstOrDefault().Value,
                Secret = BittrexData.Secret,
                Simulate = Convert.ToBoolean(ConfigParameters.Where(x => x.Key == _SIMULATE).FirstOrDefault().Value)
            };
        }

        protected void ValidateDictionary()
        {

            if (ConfigParameters == null)
                throw new Exception("Config not specified for Bittrex Account Manager!");

            if (!ConfigParameters.Any(x => x.Key == _ACCOUNT_NUMBER))
                throw new Exception(string.Format("Config parameter not specified for Account Number!:{0}", _ACCOUNT_NUMBER));


            if (!ConfigParameters.Any(x => x.Key == _MARKET_DATA_QUOTE_CURRENCY))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Account Manager!:{0}", _MARKET_DATA_QUOTE_CURRENCY));


            if (!ConfigParameters.Any(x => x.Key == _CONFIG_CONNECTION_STRING))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Data Connection String!:{0}", _CONFIG_CONNECTION_STRING));


            if (!ConfigParameters.Any(x => x.Key == _SIMULATE))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Account Manager!:{0}", _SIMULATE));

            try
            {
                bool test = Convert.ToBoolean(ConfigParameters.Where(x => x.Key == _SIMULATE).FirstOrDefault().Value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Invalid formar for config parameter {0} for Bittrex Account Manager!:{1}", _SIMULATE, ex.Message));
            }
        }

        protected void LoadConfig()
        {
            string bittrexConfigDataBaseCS = ConfigParameters.Where(x => x.Key == _CONFIG_CONNECTION_STRING).FirstOrDefault().Value;

            AccountBittrexDataManager accountBittrexDataManager = new AccountBittrexDataManager(bittrexConfigDataBaseCS);

            int accountNumber = Convert.ToInt32(ConfigParameters.Where(x => x.Key == _ACCOUNT_NUMBER).FirstOrDefault().Value);

            BittrexData = accountBittrexDataManager.GetByAccountNumber(accountNumber);

            if (BittrexData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al exchange Bittrex para la cuenta número {0}", accountNumber));

        }

        public bool MarketExists( string currency,string quoteCurrency)
        {
            Exchange exchange = new Exchange();

            ExchangeContext ctx = new ExchangeContext();
            ctx.QuoteCurrency = quoteCurrency;

            exchange.Initialise(ctx);

            try
            {
                GetMarketSummaryResponse resp = exchange.GetMarketSummary(currency);

                return resp != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region IMarketDataReferenceHandler Methods

        public MarketData GetMarketData(string symbol)
        {
          
            ExchangeContext = GetContext();
            Exchange.Initialise(ExchangeContext);

            if (MarketExists(symbol, ExchangeContext.QuoteCurrency))
            {
                GetMarketSummaryResponse summary = Exchange.GetMarketSummary(symbol);

                MarketData md = new MarketData();

                md.BestAskPrice = Convert.ToDouble(summary.Ask);
                md.NominalVolume = Convert.ToDouble(summary.BaseVolume);
                md.BestBidPrice = Convert.ToDouble(summary.Bid);
                md.TradingSessionHighPrice = Convert.ToDouble(summary.High);
                md.TradingSessionLowPrice = Convert.ToDouble(summary.Low);
                md.Trade = Convert.ToDouble(summary.Last);
                md.BestBidSize = Convert.ToInt64(summary.OpenBuyOrders);
                md.BestAskSize = Convert.ToInt64(summary.OpenSellOrders);
                md.MDEntryDate = summary.TimeStamp;
                md.TradeVolume = Convert.ToDouble(summary.Volume);

                return md;

            }
            else
                throw new Exception(string.Format("Could not find market for pair {0}-{1}", symbol, ExchangeContext.QuoteCurrency));
          
           
        }

        #endregion
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.CLS.CryptoCompare.Common.DTO;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntitie;
using zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Interface;

namespace zHFT.StrategyHandler.CLS.CryptoCompare.DAL
{
    public class MarketCapProviderManager : BaseAPIManager, IMarketCapProvider
    {
        #region Private Static Consts

        private static string _SYMBOL = "fsyms";
        private static string _QUOTE_SYMBOL = "tsyms";
        private static string _AGGREGATE_KEY = "aggregate";
        private static string _LIMIT_KEY = "limit";
        private static string _TO_TIMESTAMP = "toTs";

        private static string _QUOTE_CURRENCY = "USD";

        private static string _SYMBOLS_FULL_DATA = "data/pricemultifull";

        #endregion

        #region Protected Attributes

        protected string URL { get; set; }

        protected string ApiKey { get; set; }

        #endregion

        #region Constructors

        public MarketCapProviderManager(string pApiKey,string pURL)
        {
            URL = pURL;

            ApiKey = pApiKey;
        }


        #endregion


        #region Public Methods

        public CryptoCurrency GetCryptoCurrencyData(string symbol, string currency)
        {
            try
            {

                string param = "";
                param += string.Format("{0}={1}&", _SYMBOL, symbol);
                param += string.Format("{0}={1}&", _QUOTE_SYMBOL, currency);

                string url = string.Format("{0}{1}?{2}", URL, _SYMBOLS_FULL_DATA, param);

                string resp = DoGetJson(url);

                ResponseDTO respDTO = JsonConvert.DeserializeObject<ResponseDTO>(resp);

                if (respDTO.Response == "Error")
                    throw new Exception(respDTO.Message);

                Dictionary<string, Dictionary<string, MarketDataDisplayDTO>> displayData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, MarketDataDisplayDTO>>>(respDTO.DISPLAY.ToString());

                Dictionary<string, Dictionary<string, MarketDataRawDTO>> rawData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, MarketDataRawDTO>>>(respDTO.RAW.ToString());

                if (displayData.ContainsKey(symbol) &&rawData.ContainsKey(symbol))
                {
                    Dictionary<string, MarketDataDisplayDTO> symbolDislayData = displayData[symbol];
                    Dictionary<string, MarketDataRawDTO> symbolRawData = rawData[symbol];

                    if (symbolDislayData.ContainsKey(currency) && symbolRawData.ContainsKey(currency))
                    {
                        MarketDataDisplayDTO marketDataDisplayDTO = symbolDislayData[currency];
                        MarketDataRawDTO marketDataRawDTO = symbolRawData[currency];
                        return new CryptoCurrency()
                        {
                            Symbol = symbol,
                            MarketCap = marketDataRawDTO.MKTCAP,
                            MarketCapDesc = marketDataDisplayDTO.MKTCAP
                        };
                    }
                    else
                        return new CryptoCurrency()
                        {
                            Symbol = symbol,
                            MarketCap = 0,
                            MarketCapDesc = "??"
                        };
                }
                else
                    return  new CryptoCurrency()
                        {
                            Symbol = symbol,
                            MarketCap = 0,
                            MarketCapDesc = "??"
                        };
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("@CryptoComparePricesManager: Could not process prices for symbol {0}:{1}", symbol, ex.Message));
            }
        }



        #endregion
    }
}

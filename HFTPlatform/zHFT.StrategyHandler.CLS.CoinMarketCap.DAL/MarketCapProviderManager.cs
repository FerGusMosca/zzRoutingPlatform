using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Interface;

namespace zHFT.StrategyHandler.CLS.CoinMarketCap.DAL
{
    public class MarketCapProviderManager : BaseAPIManager, IMarketCapProvider
    {
        #region Private Static Consts 

        private static string _CRYPTO_MAIN_MARKET_DATA_PATH="v1/cryptocurrency/listings/latest";

        #endregion


        #region Protected Attributes

        protected string URL { get; set; }

        #endregion

        #region Constructors

        public MarketCapProviderManager(string pApiKey,string pURL)
            : base(pApiKey)
        {
            URL = pURL;
        }

        #endregion

        #region Public Methods

        public decimal GetMarketCap(string symbol, string currency)
        {
            try
            {
                string url = string.Format("{0}{1}", URL, _CRYPTO_MAIN_MARKET_DATA_PATH);
                string param = string.Format("{0},{1}", currency, symbol);

                string resp = DoGetJson(url, param);
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            
            }
        }

        #endregion
    }
}

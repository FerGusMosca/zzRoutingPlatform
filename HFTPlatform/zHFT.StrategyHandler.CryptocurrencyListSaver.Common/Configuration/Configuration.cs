using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {
        #region Public Attributes

        public string SecuritiesAccessLayerConnectionString { get; set; }

        public string CryptosInPortfolioConnectionString { get; set; }

        public int MaxWaitingTimeForMarketDataRequest { get; set; }

        public bool SaveNewSecurities { get; set; }

        public string MarketCapProvider { get; set; }

        public string MarketCapProviderAPIKey { get; set; }

        public string MarketCapProviderURL { get; set; }

        public string MarketCapCurrency { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {

            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }


            if (string.IsNullOrEmpty(SecuritiesAccessLayerConnectionString))
            {
                result.Add("SecuritiesAccessLayerConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(CryptosInPortfolioConnectionString))
            {
                result.Add("CryptosInPortfolioConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(MarketCapProvider))
            {
                result.Add("MarketCapProvider");
                resultado = false;
            }

            if (string.IsNullOrEmpty(MarketCapProviderAPIKey))
            {
                result.Add("MarketCapProviderAPIKey");
                resultado = false;
            }

            if (string.IsNullOrEmpty(MarketCapProviderURL))
            {
                result.Add("MarketCapProviderURL");
                resultado = false;
            }


            if (string.IsNullOrEmpty(MarketCapCurrency))
            {
                result.Add("MarketCapCurrency");
                resultado = false;
            }

            if (MaxWaitingTimeForMarketDataRequest<=0)
            {
                result.Add("MaxWaitingTimeForMarketDataRequest");
                resultado = false;
            }
         
            return resultado;

        }


        #endregion
    }
}

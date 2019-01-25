using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.StrategyHandler.PairTradingDemo.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {
        #region Public Attributes

        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        public string PairTradingRequestURL { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

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

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouterConfigFile))
            {
                result.Add("OrderRouterConfigFile");
                resultado = false;
            }

            if (string.IsNullOrEmpty(PairTradingRequestURL))
            {
                result.Add("PairTradingRequestURL");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Currency))
            {
                result.Add("Currency");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add("Exchange");
                resultado = false;
            }


            return resultado;
        }

        #endregion
    }
}

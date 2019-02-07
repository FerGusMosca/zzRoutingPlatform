using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Configuration;

namespace zHFT.OrderImbSimpleCalculator.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {
        #region Public Attributes

        public string ConnectionString { get; set; }

        [XmlArray]
        [XmlArrayItem(ElementName = "Symbol")]
        public List<string> StocksToMonitor { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        public bool ResetOnPersistance { get; set; }

        public string SaveEvery { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (StocksToMonitor.Count==0)
            {
                result.Add("StocksToMonitor");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                result.Add("ConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SaveEvery))
            {
                result.Add("SaveEvery");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
                resultado = false;
            }

            if (string.IsNullOrEmpty(OrderRouter))
            {
                result.Add("OrderRouter");
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

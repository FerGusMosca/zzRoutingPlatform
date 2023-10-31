using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;

namespace zHFT.BidAskArbitrage.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {
        #region Public Attributes

        [XmlArray]
        [XmlArrayItem(ElementName = "Symbols")]
        public List<string> PairsToMonitor { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (PairsToMonitor.Count == 0)
            {
                result.Add("PairsToMonitor");
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;

namespace zHFT.StrategyHandler.SecurityListSaver.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {
        #region Public Attributes

        public string Country { get; set; }

        public string SecuritiesAccessLayerConnectionString { get; set; }

        public string SecuritiesMarketTranslator { get; set; }

        public bool? SaveNewSecurities { get; set; }

        [XmlArray]
        [XmlArrayItem(ElementName = "SecurityType")]
        public List<string> SecurityTypes { get; set; }

        [XmlArray]
        [XmlArrayItem(ElementName = "Market")]
        public List<string> Markets { get; set; }


        #endregion

        public override bool CheckDefaults(List<string> result)
        {

            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }


            if (string.IsNullOrEmpty(Country))
            {
                result.Add("Country");
                resultado = false;
            }

            if (!SaveNewSecurities.HasValue)
            {
                result.Add("SaveNewSecurities");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SecuritiesAccessLayerConnectionString))
            {
                result.Add("SecuritiesAccessLayerConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SecuritiesMarketTranslator))
            {
                result.Add("SecuritiesMarketTranslator");
                resultado = false;
            }

            return resultado;
            
        }
    }
}

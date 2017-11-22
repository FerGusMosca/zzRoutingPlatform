using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Configuration;


namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration
{
    public class ConfigKey
    {
        [XmlElement(ElementName = "Key")]
        public string Key { get; set; }

        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }
  
    }


    public class Configuration : StrategyConfiguration, IConfiguration
    {
        #region Public Attributes

        public string InstructionsAccessLayerConnectionString { get; set; }

        public long AccountNumber { get; set; }

        public int RoutingUpdateInMilliseconds { get; set; }

        public string AccoutReferenceHandler { get; set; }

        public string AccountManagerAccessLayer { get; set; }

        public string InstructionManagerAccessLayer { get; set; }

        public string PositionManagerAccessLayer { get; set; }

        [XmlArray(ElementName = "AccountManagerConfig")]
        [XmlArrayItem(ElementName = "Config")]
        public List<ConfigKey> AccountManagerConfig { get; set; }

        #endregion

        #region Private Methods

        public bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            resultado = base.CheckDefaults(result);

            if (string.IsNullOrEmpty(InstructionsAccessLayerConnectionString))
            {
                result.Add("InstructionsAccessLayerConnectionString");
                resultado = false;
            }

            if (AccountNumber <= 0)
            {
                result.Add("AccountNumber");
                resultado = false;
            }

            if (RoutingUpdateInMilliseconds <= 0)
            {
                result.Add("RoutingUpdateInMilliseconds");
                resultado = false;
            }

            if (string.IsNullOrEmpty(AccoutReferenceHandler))
            {
                result.Add("AccoutReferenceHandler");
                resultado = false;
            }

            if (string.IsNullOrEmpty(AccountManagerAccessLayer))
            {
                result.Add("AccoutReferenceHandler");
                resultado = false;
            }

            if (string.IsNullOrEmpty(InstructionManagerAccessLayer))
            {
                result.Add("AccoutReferenceHandler");
                resultado = false;
            }


            return resultado;
        }

        #endregion
    }
}

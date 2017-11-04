using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Configuration;

namespace zHFT.StrategyHandler.SingleStockBuyer.Common.Configuration
{
    public class Configuration : StrategyConfiguration, IConfiguration
    {
        #region Public Attributes

        [XmlElement(ElementName = "Contract")]
        public List<Contract> ContractList { get; set; }

        #endregion

        #region Public Methods

        public bool CheckDefaults(List<string> result)
        {
            
            bool resultado = true;

            resultado = base.CheckDefaults(result);
            return resultado;
        }

        #endregion
    }
}

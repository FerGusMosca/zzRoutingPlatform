using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Configuration;

namespace zHFT.StrategyHandler.OptionsContractSaver.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string OptionsAccessLayerConnectionString { get; set; }

        public string EFOptionsAccessLayerConnectionString { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(OptionsAccessLayerConnectionString))
            {
                result.Add("OptionsAccessLayerConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(EFOptionsAccessLayerConnectionString))
            {
                result.Add("EFOptionsAccessLayerConnectionString");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

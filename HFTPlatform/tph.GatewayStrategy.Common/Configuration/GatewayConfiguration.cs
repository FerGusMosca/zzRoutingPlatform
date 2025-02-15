using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common;

namespace tph.GatewayStrategy.Common.Configuration
{
    public class GatewayConfiguration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes


        public string IncomingModule { get; set; }

        public string IncomingModuleConfigFile { get; set; }

        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        #endregion


        #region Overriden Methods
        public override bool CheckDefaults(List<string> result)
        {
            return true;
        }


        #endregion
    }
}

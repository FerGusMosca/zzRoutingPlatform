using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.StrategyHandler.Common;

namespace tph.ConsoleStrategy.Common.Configuration
{
    public class ConsoleConfiguration : BaseStrategyConfiguration
    {

        public string EconomicDataModule { get; set; }

        public string EconomicDataModuleConfigFile { get; set; }

        public override bool CheckDefaults(List<string> result)
        {
            if (string.IsNullOrEmpty(OrderRouter))
                return false;
            else if (string.IsNullOrEmpty(OrderRouterConfigFile))
                return false;

            else return true;
        }
    }
}

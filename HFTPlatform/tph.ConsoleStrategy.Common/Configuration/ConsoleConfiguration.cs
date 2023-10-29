using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;

namespace tph.ConsoleStrategy.Common.Configuration
{
    public class ConsoleConfiguration : BaseConfiguration
    {

        #region Public Attributes

        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        #endregion

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

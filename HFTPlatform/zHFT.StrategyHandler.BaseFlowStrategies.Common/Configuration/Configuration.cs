using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.BaseFlowStrategies.Common.Configuration
{
    public class Configuration : zHFT.StrategyHandler.Common.Configuration.StrategyConfiguration
    {
        #region Public Attributes

        public IList<string> InstrumentList { get; set; }

        public IList<string> MarketList { get; set; }

        #endregion
    }
}

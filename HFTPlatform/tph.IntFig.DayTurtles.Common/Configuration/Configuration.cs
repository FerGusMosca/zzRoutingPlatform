using tph.TrendlineTurtles.Common.Configuration;
using zHFT.StrategyHandler.Common;

namespace tph.IntFig.DayTurtles.Common.Configuration
{
    public class Configuration: TrendlineConfiguration
    {
        #region Public Attributes

        public double ProximityPctToTriggerTrade { get; set; }

        #endregion
    }
}
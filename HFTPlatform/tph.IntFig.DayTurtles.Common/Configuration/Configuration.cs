using tph.TrendlineTurtles.Common.Configuration;
using zHFT.StrategyHandler.Common;

namespace tph.IntFig.DayTurtles.Common.Configuration
{
    public class Configuration: TrendlineConfiguration
    {
        #region Public Attributes

        public int CloseWindow { get; set; }
        
        public decimal ProximityToTriggerTrade { get; set; }

        public string ConnectionString { get; set; }
        

        #endregion
    }
}
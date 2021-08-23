using zHFT.StrategyHandler.Common;

namespace tph.BOBDayTurtles.Common.Configuration
{
    public class Configuration : BaseStrategyConfiguration
    {
        #region Public Attributes

        public int OpenWindow { get; set; }

        public int CloseWindow { get; set; }

        public string ConnectionString { get; set; }
        
        public int InnerTrendlinesSpan { get; set; }
        
        public int OuterTrendlineSpan { get; set; }
        
        public double PerforationThresholds { get; set; }
        
        public double MaxLongPositiveSlope { get; set; }
        
        public double MaxLongNegativeSlope { get; set; }
        
        public double MaxShortPositiveSlope { get; set; }
        
        public double MaxShortNegativeSlope { get; set; }

        #endregion
    }
}
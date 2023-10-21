using tph.DayTurtles.Common.Configuration;
using zHFT.StrategyHandler.Common;

namespace tph.TrendlineTurtles.Common.Configuration
{
    public class TrendlineConfiguration: DayTurtlesConfiguration
    {
        #region Public Attr

        public int InnerTrendlinesSpan { get; set; }
        
        public int OuterTrendlineSpan { get; set; }
        
        public double PerforationThresholds { get; set; }
        
        public double MaxLongPositiveSlope { get; set; }
        
        public double MaxLongNegativeSlope { get; set; }
        
        public double MaxShortPositiveSlope { get; set; }
        
        public double MaxShortNegativeSlope { get; set; }
        
        public bool RecalculateTrendlines { get; set; }

        public int HistoricalPricesPeriod { get; set; }

        #endregion
    }
}
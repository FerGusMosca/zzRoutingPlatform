using tph.TrendlineTurtles.BusinessEntities;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.IntFig.DayTurtles.BusinessEntities
{
    public class TradIntFigTurtlesPosition:TradingPosition
    {
        
        #region Public Attributes
        
        public Trendline OpeningTrendline { get; set; }
        
        #endregion
        public override void DoCloseTradingPosition(TradingPosition trdPos)
        {
            
        }
    }
}
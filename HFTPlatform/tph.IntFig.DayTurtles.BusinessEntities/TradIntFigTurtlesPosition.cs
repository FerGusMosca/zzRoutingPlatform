using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.IntFig.DayTurtles.BusinessEntities
{
    public class TradIntFigTurtlesPosition:PortfTrendlineTurtlesPosition
    {
        
        #region Public Attributes
        
        public Trendline OpeningTrendline { get; set; }
        
        #endregion
        public override void DoCloseTradingPosition(PortfolioPosition trdPos)
        {
            
        }
    }
}
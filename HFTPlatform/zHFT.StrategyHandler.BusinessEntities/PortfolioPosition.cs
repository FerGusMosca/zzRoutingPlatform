using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public class PortfolioPosition
    {
        
        #region Public Attributes
        
        public Security Security { get; set; }
        
        public bool Closing { get; set; }
        
        
        public int? DecimalRounding { get; set; }
        
        #endregion
        
        #region public Methods
        
        public virtual string SignalTriggered()
        {
            return "";

        }
        
        #endregion
        
        
    }
}
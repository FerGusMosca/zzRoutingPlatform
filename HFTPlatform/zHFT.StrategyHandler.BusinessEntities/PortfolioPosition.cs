using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public class PortfolioPosition
    {
        
        #region Public Static Consts

        public static string _CANLDE_REF_PRICE_TRADE = "TRADE";
        public static string _CANLDE_REF_PRICE_CLOSE = "CLOSE";
        
        #endregion
        
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

        public bool IsClosing()
        {
            return Closing;
        }
        
        #endregion
        
        
    }
}
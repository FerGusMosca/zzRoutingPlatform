namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class MarketDataDTO:zHFT.Main.BusinessEntities.Market_Data.MarketData
    {
        #region Constructors

        public MarketDataDTO(zHFT.Main.BusinessEntities.Market_Data.MarketData md)
        {
            MarketData = md;
        }

        #endregion
        
        #region Public Attributes 
        
        public zHFT.Main.BusinessEntities.Market_Data.MarketData MarketData { get; set; }
        
        public string Msg = "MarketDataMsg";
        
        #endregion
    }
}
using System.Collections.Generic;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class HistoricalPricesDTO
    {
        #region Public Attributes
        
        public string Msg = "HistoricalPricesMsg";

        public string Symbol { get; set; }
        
        public List<zHFT.Main.BusinessEntities.Market_Data.MarketData> MarketData { get; set; }

        #endregion
    }
}
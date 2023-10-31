using zHFT.Main.BusinessEntities.Market_Data;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class OrderBookDTO:OrderBook
    {
        #region Constructors

        public OrderBookDTO(OrderBook ob)
        {
            Security = ob.Security;
            Bids = ob.Bids;
            Asks = ob.Asks;
        }

        #endregion
        
        #region Public Attributes 
        
        public string Msg = "OrderBookMsg";
        
        #endregion
    }
}
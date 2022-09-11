using zHFT.Main.BusinessEntities.Market_Data;

namespace OrderBookLoaderMock.Common.DTO
{
    public class MarketDataMsg:MarketData
    {
        #region Public Attributes

        public string Msg = "MarketDataMsg";    
        
        public string Symbol { get; set; }

        #endregion
        
        #region Public Methods
        

        public override string ToString()
        {
            return string.Format(
                "Symbol={0} Open={1} High={2} Low={3} Close={4} Trade={5} BestBidPx={6} BestAskPx={7} Volume={8}",
                Security.Symbol, OpeningPrice, TradingSessionHighPrice,
                TradingSessionLowPrice, ClosingPrice, Trade, BestBidPrice,
                BestAskPrice, TradeVolume);
        }

        #endregion
    }
}
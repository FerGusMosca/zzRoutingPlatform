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
                $"Date={MDEntryDate} Symbol={Security.Symbol} Open={OpeningPrice} High={TradingSessionHighPrice} Low={TradingSessionLowPrice} Close={ClosingPrice} Trade={Trade} BestBidPx={BestBidPrice} BestAskPx={BestAskPrice} Volume={TradeVolume}");
            
        }

        #endregion
    }
}
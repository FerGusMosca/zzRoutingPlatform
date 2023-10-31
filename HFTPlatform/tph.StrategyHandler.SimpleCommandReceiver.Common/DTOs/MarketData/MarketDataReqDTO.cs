namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class MarketDataReqDTO:WebSocketMessage
    {
        #region Public Attributes
        
        public string Symbol { get; set; }
        
        public string SecurityType { get; set; }
        
        public string Currency { get; set; }
        
        public string MDReqId { get; set; }
        
        public string SubscriptionRequestType { get; set; }
        
        #endregion
    }
}
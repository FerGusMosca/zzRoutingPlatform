namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class HistoricalPricesReqAckDTO:WebSocketMessage
    {
        
        #region Constructors

        public HistoricalPricesReqAckDTO()
        {
            Msg = "HistoricalPricesReqAck";
        }

        #endregion
        
        #region Public Attributes

        public string UUID { get; set; }

        public string ReqId { get; set; }

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion
    }
}
namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class CancelOrderReq:WebSocketMessage
    {
        #region Public Attributes
        
        public  string OrigClOrderId{get; set; }
        
        public  string ClOrderId{get; set; }
        
        #endregion
    }
}
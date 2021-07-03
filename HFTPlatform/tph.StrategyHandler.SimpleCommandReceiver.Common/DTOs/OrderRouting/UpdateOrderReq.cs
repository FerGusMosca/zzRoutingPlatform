namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class UpdateOrderReq:WebSocketMessage
    {
        #region Public Attributes
        
        public string Symbol { get; set; }
        
        public  string OrigClOrderId{get; set; }
        
        public  string ClOrderId{get; set; }
        
        public  string OrderId{get; set; }
        
        public  double? Qty{get; set; }
        
        public  double? Price{get; set; }
        
        #endregion
    }
}
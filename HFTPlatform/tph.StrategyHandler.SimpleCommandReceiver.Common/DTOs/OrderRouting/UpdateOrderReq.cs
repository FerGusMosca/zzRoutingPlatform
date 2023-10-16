namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class UpdateOrderReq:WebSocketMessage
    {
        #region Constructors

        public UpdateOrderReq()
        {
            Msg = "UpdateOrderReq";
        }

        #endregion
        
        #region Public Attributes
        
        public string Symbol { get; set; }
        
        public  string OrigClOrdId{get; set; }
        
        public  string ClOrdId{get; set; }
        
        public  string OrderId{get; set; }
        
        public  double? Qty{get; set; }
        
        public  double? Price{get; set; }
        
        #endregion
    }
}
namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class CancelOrderReq:WebSocketMessage
    {
        #region  Constuructors

        public CancelOrderReq()
        {
            Msg = "CancelOrderReq";
        }

        #endregion
        
        #region Public Attributes
        
        public  string OrigClOrderId{get; set; }
        
        public  string ClOrderId{get; set; }
        
        #endregion
    }
}
namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class OrderMassStatusRequestAck:WebSocketMessage
    {
        #region Constructors

        public OrderMassStatusRequestAck()
        {
            Msg = "OrderMassStatusRequestAck";
        }

        #endregion
        
        #region Public Attributes

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion
    }
}
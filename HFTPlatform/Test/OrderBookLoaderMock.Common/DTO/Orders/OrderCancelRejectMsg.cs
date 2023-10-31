using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;

namespace OrderBookLoaderMock.Common.DTO.Orders
{
    public class OrderCancelRejectMsg: WebSocketMessage
    {
        #region Constructors

        public OrderCancelRejectMsg()
        {
            Msg = "OrderCancelRejectMsg";
        }

        #endregion
        
        #region Public Attributes
        
        public string ClOrdId { get; set; }
        
        public string OrigClOrdId { get; set; }
        
        public string Text { get; set; }
        
        public string ResponseTo { get; set; }
        
        #endregion
        
        #region Public Methods

        public override string ToString()
        {
            return string.Format("Cancel Reject for OrigClOrdId={0} ClOrdId={1} ResponseTo={2} Text={3}",
                OrigClOrdId, ClOrdId, ResponseTo, Text);
        }

        #endregion
    }
}
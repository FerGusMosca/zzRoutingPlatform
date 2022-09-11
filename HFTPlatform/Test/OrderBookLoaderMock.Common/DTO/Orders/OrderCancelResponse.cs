using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;

namespace OrderBookLoaderMock.Common.DTO.Orders
{
    public class OrderCancelResponse:WebSocketMessage
    {
        #region Constructors


        public OrderCancelResponse()
        {
            Msg = "OrderCancelResponse";
        }

        #endregion
        
        #region Public Attributes
        
        public bool Success { get; set; }
        
        public string Error { get; set; }
        
        #endregion
        
        #region Public Methods

        public override string ToString()
        {
            return string.Format(" OrderCancelResponse: Success={0} Error={1}", Success, Error != null ? Error : "no");
        }

        #endregion
    }
}
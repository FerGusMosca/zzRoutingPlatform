using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;

namespace OrderBookLoaderMock.Common.DTO.Orders
{
    public class NewOrderResponse:WebSocketMessage
    {
        #region Constructors


        public NewOrderResponse()
        {
            Msg = "NewOrderResponse";
        }

        #endregion
        
        #region Public Attributes
        
        public bool Success { get; set; }
        
        public string Error { get; set; }
        
        #endregion
        
        #region Public Methods

        public override string ToString()
        {
            return string.Format(" NewOrderResponse: Success={0} Error={1}", Success, Error != null ? Error : "no");
        }

        #endregion
    }
}
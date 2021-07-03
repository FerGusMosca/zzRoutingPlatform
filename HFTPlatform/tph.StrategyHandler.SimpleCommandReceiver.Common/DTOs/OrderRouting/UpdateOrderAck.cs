namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class UpdateOrderAck:WebSocketMessage
    {
        #region Public Attributes

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion
    }
}
namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class CancelOrderAck:WebSocketMessage
    {
        #region Public Attributes

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion
    }
}
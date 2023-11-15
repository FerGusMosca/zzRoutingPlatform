namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class CancelOrderAck:WebSocketMessage
    {
        #region Public Attributes

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return Success ? " CancelOrderAck successful!" : $"CancelOrderAck error:{Error}";
        }

        #endregion
    }
}
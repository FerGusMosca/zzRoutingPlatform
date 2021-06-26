namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class RouteOrderAck:WebSocketMessage
    {
        #region Public Attributes

        public string UUID { get; set; }

        public string ReqId { get; set; }

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion
    }
}
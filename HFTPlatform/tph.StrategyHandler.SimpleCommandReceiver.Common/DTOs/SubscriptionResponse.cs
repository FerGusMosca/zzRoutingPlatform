namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs
{
    public class SubscriptionResponse:WebSocketMessage
    {
        public string Service { get; set; }

        public string UUID { get; set; }

        public string ServiceKey { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }
}
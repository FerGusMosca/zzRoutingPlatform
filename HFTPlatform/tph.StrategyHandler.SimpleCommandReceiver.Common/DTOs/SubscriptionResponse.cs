namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs
{
    public class SubscriptionResponse:WebSocketMessage
    {

        #region Public Attributes
        public string Service { get; set; }

        public string UUID { get; set; }

        public string ServiceKey { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return Success ? $" SubscriptionResponse to Svc={Service} w/Service Key={ServiceKey} successful!" : $"SubscriptionResponse to Svc={Service} w/Service Key={ServiceKey}  error:{Message} ";
        }

        #endregion
    }
}
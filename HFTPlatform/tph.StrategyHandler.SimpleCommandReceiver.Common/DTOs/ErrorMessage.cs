namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs
{
    public class ErrorMessage:WebSocketMessage
    {
        public string Error { get; set; }
    }
}
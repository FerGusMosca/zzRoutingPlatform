namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class CancelAllReq:WebSocketMessage
    {
        public string Reason { get; set; }
    }
}
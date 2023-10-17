namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs
{
    public class UnknownMessage:WebSocketMessage
    {
        public  string Resp { get; set; }
        
        public  string Reason { get; set; }
    }
}
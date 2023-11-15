namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs
{
    public class UnknownMessage:WebSocketMessage
    {
        public  string Resp { get; set; }
        
        public  string Reason { get; set; }

        public override string ToString()
        {
            return $"Unkn Resp={Resp} Reason={Reason}";
        }
    }
}
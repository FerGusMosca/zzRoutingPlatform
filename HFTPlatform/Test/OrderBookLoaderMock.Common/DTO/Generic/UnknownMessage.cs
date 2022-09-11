using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;

namespace OrderBookLoaderMock.Common.DTO.Generic
{
    public class UnknownMessage: WebSocketMessage
    {
        public string Resp { get; set; }

        public string Reason { get; set; }
    }
}
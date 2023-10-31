using OrderBookLoaderMock.Common.DTO;
using OrderBookLoaderMock.Common.DTO.Orders;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;

namespace OrderBookLoaderMock.Common.Interfaces
{
    public interface IMarketDataPublication
    {
        void ProcessEvent(WebSocketMessage msg);
        
        void OnMarketData(MarketDataMsg msg);
        
        
    }
}
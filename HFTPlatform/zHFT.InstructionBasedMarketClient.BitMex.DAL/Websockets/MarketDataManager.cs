using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets;

namespace zHFT.InstructionBasedMarketClient.BitMex.DAL.Websockets
{
    public class MarketDataManager : BaseManager
    {
        #region Constructors

        public MarketDataManager(string pWebSocketUrl = null, bool connectWebSocket = false)
        {
            WebSocketURL = pWebSocketUrl;

            if (connectWebSocket)
            {
                ConnectSubscriptions().Wait();
            }
        }

        #endregion

        #region Public Methods

        public void SubscribeOrderBookL2(string symbol)
        {
            WebSocketSubscriptionRequest request = new WebSocketSubscriptionRequest()
            {
                op = "subscribe",
                args = new string[] { string.Format("{0}:{1}", _ORDERBOOK_L2, symbol) }

            };

            InvokeWebSocket(request).Wait();
            
        }


        public void SubscribeTrades(string symbol, string quoteSymbol)
        {
            WebSocketSubscriptionRequest request = new WebSocketSubscriptionRequest()
            {
                op = "subscribe",
                args = new string[] { string.Format("{0}:{1}{2}", _TRADE, symbol, quoteSymbol) }

            };

            InvokeWebSocket(request).Wait();
        }


        public void SubscribeTrades(string symbol)
        {
            WebSocketSubscriptionRequest request = new WebSocketSubscriptionRequest()
            {
                op = "subscribe",
                args = new string[] { string.Format("{0}:{1}", _TRADE, symbol) }

            };

            InvokeWebSocket(request).Wait();
        }

        public void SubscribeTradeBins(string symbol)
        {
            WebSocketSubscriptionRequest request = new WebSocketSubscriptionRequest()
            {
                op = "subscribe",
                args = new string[] { string.Format("{0}:{1}", _1_DAY_TRADE_BINS, symbol) }

            };
            InvokeWebSocket(request).Wait();
        }


        #endregion
    }
}

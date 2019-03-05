using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets.Events;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.BitMex.DAL.Websockets
{
    public class BaseManager : zHFT.FullMrktConnectivity.BitMex.DAL.WebSockets.BaseManager, IWebsocketManager
    {

        #region Constructors

        public BaseManager(string pWebSocketURL, UserCredentials pUserCredentials): base(pWebSocketURL,pUserCredentials)
        {
           
        }

        public BaseManager()
        {
            PendingRequestResponse = false;
        }

        #endregion

        #region Protected Methods

        protected override void DoRunLoopSubscriptions(string resp)
        {
            WebSocketSubscriptionEvent eventSubscr = JsonConvert.DeserializeObject<WebSocketSubscriptionEvent>(resp);

            if (ResponseRequestSubscriptions.ContainsKey(eventSubscr.GetSubscriptionEvent()))
            {

                if (eventSubscr.table == _ORDERBOOK_L2 && EventSubscriptions.ContainsKey(eventSubscr.GetSubscriptionEvent()))
                {
                    WebSocketOrderBookL2Event orderBookL2Event = JsonConvert.DeserializeObject<WebSocketOrderBookL2Event>(resp);

                    WebSocketSubscriptionEvent subscrEvent = EventSubscriptions[eventSubscr.GetSubscriptionEvent()];

                    subscrEvent.RunEvent(orderBookL2Event);
                }
                else if (eventSubscr.table == _TRADE && EventSubscriptions.ContainsKey(eventSubscr.GetSubscriptionEvent()))
                {
                    WebSocketTradeEvent tradeEvent = JsonConvert.DeserializeObject<WebSocketTradeEvent>(resp);

                    WebSocketSubscriptionEvent subscrEvent = EventSubscriptions[eventSubscr.GetSubscriptionEvent()];

                    subscrEvent.RunEvent(tradeEvent);
                }
                else if (eventSubscr.table == _1_DAY_TRADE_BINS && EventSubscriptions.ContainsKey(eventSubscr.GetSubscriptionEvent()))
                {
                    //TODO: I have to ask BitMex why aren't they retrieveing high and low for the 1 day bin
                    //and they are for the 1 hour bin
                }
                else
                {
                    //Log what we are receiving here because we are getting events that we didn't expect

                }
            }
        
        }

        #endregion
    }
}

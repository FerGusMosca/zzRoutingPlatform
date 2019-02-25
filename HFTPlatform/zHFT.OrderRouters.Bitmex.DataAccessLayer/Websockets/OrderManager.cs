using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth;

namespace zHFT.OrderRouters.Bitmex.DataAccessLayer.Websockets
{
    public class OrderManager : BaseManager
    {
        #region Constructors

        public OrderManager(string pWebSocketUrl, UserCredentials pUserCredentials)
            : base(pWebSocketUrl, pUserCredentials)
        {
            AuthenticateSubscriptions().Wait();
        }

        #endregion

        #region Public Methods

        //We receive an execution report for every of OUR order that were sent to the market
        public void SubscribeOrders()
        {
            WebSocketSubscriptionRequest request = new WebSocketSubscriptionRequest()
            {
                op = "subscribe",
                args = new string[] { string.Format("{0}", _ORDER) }

            };

            InvokeWebSocket(request).Wait();
        }

        //We receive an execution report for every of OUR order that were sent to the market
        public void SubscribeExecutions()
        {
            WebSocketSubscriptionRequest request = new WebSocketSubscriptionRequest()
            {
                op = "subscribe",
                args = new string[] { string.Format("{0}", _EXECUTIONS) }

            };

            InvokeWebSocket(request).Wait();
        }


        #endregion
    }
}

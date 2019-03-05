using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth;
using zHFT.Main.Common.Interfaces;
using zHFT.OrderRouters.Bitmex.Common.DTO.Events;

namespace zHFT.OrderRouters.Bitmex.DataAccessLayer.Websockets
{
    public class BaseManager : zHFT.FullMrktConnectivity.BitMex.DAL.WebSockets.BaseManager, IWebsocketManager
    {
        #region Constructors

        public BaseManager(string pWebSocketURL, UserCredentials pUserCredentials)
            : base(pWebSocketURL, pUserCredentials)
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

                if (eventSubscr.table == _ORDER && EventSubscriptions.ContainsKey(eventSubscr.GetSubscriptionEvent()))
                {
                    WebSocketExecutionReportEvent execReportEvent = JsonConvert.DeserializeObject<WebSocketExecutionReportEvent>(resp);

                    WebSocketSubscriptionEvent subscrEvent = EventSubscriptions[eventSubscr.GetSubscriptionEvent()];

                    subscrEvent.RunEvent(execReportEvent);
                }
                else if (eventSubscr.table == _EXECUTIONS && EventSubscriptions.ContainsKey(eventSubscr.GetSubscriptionEvent()))
                {
                    WebSocketExecutionReportEvent execReportEvent = JsonConvert.DeserializeObject<WebSocketExecutionReportEvent>(resp);

                    WebSocketSubscriptionEvent subscrEvent = EventSubscriptions[eventSubscr.GetSubscriptionEvent()];

                    subscrEvent.RunEvent(execReportEvent);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets
{
    public delegate void OnSubscritionEvent(WebSocketResponseMessage WebSocketResponseMessage);

    public delegate void OnEvent(WebSocketSubscriptionEvent WebSocketSubscriptionEvent);

    public class WebSocketResponseMessage
    {
        #region Public Attributes

        public string id { get; set; }

        public string topic { get; set; }

        public string table { get; set; }

        public string subscribe { get; set; }

        public event OnSubscritionEvent SubscriptionEvent;

        public event OnEvent Event;

        #endregion

        #region Public Attributes

        public bool IsResponse()
        {
            return !string.IsNullOrEmpty(subscribe);
        }


        public virtual bool IsAuthentication()
        {
            return false;
        }



        public void RunSubscritionEvent(WebSocketResponseMessage wsResp)
        {
            SubscriptionEvent(wsResp);
        }

        public void SetSubscritionEvent(OnSubscritionEvent pOnSubscriptionEvent)
        {
            SubscriptionEvent += pOnSubscriptionEvent;
        }

        public void RunEvent(WebSocketSubscriptionEvent wsResp)
        {
            Event(wsResp);
        }

        public void SetEvent(OnEvent pOnEvent)
        {
            Event += pOnEvent;
        }

        #endregion
    }
}

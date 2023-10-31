using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets
{
    public class WebSocketSubscriptionResponse : WebSocketResponseMessage
    {
        #region Public Attributes

        public bool success { get; set; }

        public int status { get; set; }

        public string error { get; set; }

        public WebSocketSubscriptionRequest request { get; set; }

        public object[] parameters { get; set; }

        #endregion

        #region Public Attributes

        public override bool IsAuthentication()
        {
            return (!IsResponse() && (request != null && request.op == "authKeyExpires"));
        }

        public string GetSubscriptionEvent()
        {
            if (subscribe == null)
                throw new Exception("Invalid state for subscription message");

            string[] pair = subscribe.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            if (pair.Length < 1)
                throw new Exception(string.Format("Invalid subscription event: {0}", subscribe));

            return pair[0];

        }

        public string GetSubscriptionAsset()
        {
            if (subscribe == null)
                throw new Exception("Invalid state for subscription message");

            string[] pair = subscribe.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            if (pair.Length < 2)
                throw new Exception(string.Format("Invalid subscription event: {0}", subscribe));

            return pair[1];
        }

        #endregion

    }
}

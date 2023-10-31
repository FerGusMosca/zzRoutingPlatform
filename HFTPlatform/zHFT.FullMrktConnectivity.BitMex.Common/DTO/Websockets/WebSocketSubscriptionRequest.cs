using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets
{
    public class WebSocketSubscriptionRequest
    {
        #region Public Attributes

        public string op { get; set; }

        public string[] args { get; set; }


        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets.Events
{
    public class WebSocketOrderBookL2Event : WebSocketSubscriptionEvent
    {
        #region Public Attributes

        public OrderBookEntry[] data { get; set; }

        #endregion
    }
}

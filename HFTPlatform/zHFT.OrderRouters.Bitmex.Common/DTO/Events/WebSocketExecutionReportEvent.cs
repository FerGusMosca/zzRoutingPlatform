using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;

namespace zHFT.OrderRouters.Bitmex.Common.DTO.Events
{
    public class WebSocketExecutionReportEvent : WebSocketSubscriptionEvent
    {
        #region Public Attributes

        public ExecutionReport[] data { get; set; }

        #endregion
    }
}

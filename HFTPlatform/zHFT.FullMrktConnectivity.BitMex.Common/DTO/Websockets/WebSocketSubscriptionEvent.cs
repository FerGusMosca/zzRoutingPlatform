using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Util;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets
{
    public class WebSocketSubscriptionEvent : WebSocketResponseMessage
    {
        #region Public Attributes

        public string[] keys { get; set; }

        public zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Util.Type types { get; set; }

        public ForeignKey foreignKey { get; set; }

        public zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Util.Attribute attributes { get; set; }

        public string action { get; set; }


        #endregion

        #region Public Methods


        public string GetSubscriptionEvent()
        {
            return table;

        }

        #endregion
    }
}

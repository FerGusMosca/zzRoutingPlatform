using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets
{
    public class WebSocketAuthenticationRequest
    {
        #region Public Attributes

        public string op { get; set; }

        public object[] args { get; set; }


        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth
{
    public class AuthenticationResult
    {
        #region Public Attributes

        public UserCredentials UserCredentials { get; set; }

        public bool Success { get; set; }

        public bool Authenticated { get; set; }

        public string ErrorMessage { get; set; }

        public int ExpiresIn { get; set; }

        public Welcome Welcome { get; set; }

        public ClientWebSocket ClientWebSocket { get; set; }

        #endregion
    }
}

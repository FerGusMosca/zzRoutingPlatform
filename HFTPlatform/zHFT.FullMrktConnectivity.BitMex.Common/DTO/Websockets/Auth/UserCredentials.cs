using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth
{
    public class UserCredentials
    {
        #region Public Attributes

        public string Id { get; set; }

        public string Topic { get; set; }

        public string BitMexID { get; set; }

        public string BitMexSecret { get; set; }

        #endregion
    }
}

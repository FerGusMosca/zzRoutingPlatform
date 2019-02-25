using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth
{
    public class Welcome
    {
        #region Public Attributes

        public bool success { get; set; }

        public string error { get; set; }

        public string info { get; set; }

        public DateTime version { get; set; }

        public DateTime timestamp { get; set; }

        public string docs { get; set; }

        public WelcomeLimit limit { get; set; }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Util
{
    public class Type
    {
        public string id { get; set; }

        public string price { get; set; }

        public string side { get; set; }

        public string size { get; set; }

        public string symbol { get; set; }
    }
}

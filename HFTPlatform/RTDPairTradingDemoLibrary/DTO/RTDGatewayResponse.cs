using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTDPairTradingDemoLibrary.DTO
{
    public class RTDGatewayResponse
    {
        public bool IsOK { get; set; }

        public string Error { get; set; }

        public string Response { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.Bittrex.Common.DTO
{
    public class MarketDataDTO
    {
        public string Symbol { get; set; }

        public double? Bid { get; set; }

        public double? Ask { get; set; }

        public double? Last { get; set; }
    }
}

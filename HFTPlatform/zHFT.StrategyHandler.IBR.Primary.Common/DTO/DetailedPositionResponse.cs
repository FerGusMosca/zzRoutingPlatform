using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.Primary.Common.DTO
{
    public class DetailedPositionResponse
    {
        public string account { get; set; }

        public decimal lastCalculation { get; set; }

        public decimal? totalDailyDiffPlain { get; set; }

        public decimal? totalMarketValue { get; set; }

        public PositionReport report { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.Primary.Common.DTO
{
    public class DetailedPositionSummary
    {
        public string account { get; set; }

        public decimal lastCalculation { get; set; }

        public decimal? totalDailyDiffPlain { get; set; }

        public decimal? totalMarketValue { get; set; }

        public Dictionary<string, Dictionary<string, DetailedPositions>> report { get; set; }
    }
}

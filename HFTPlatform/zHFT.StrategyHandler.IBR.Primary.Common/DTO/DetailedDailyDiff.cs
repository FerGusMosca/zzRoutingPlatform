using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.Primary.Common.DTO
{
    public class DetailedDailyDiff
    {
        public decimal? totalDailyDiff { get; set; }

        public decimal? buyDailyDiffPlain { get; set; }

        public decimal? sellDailyDiffPlain { get; set; }

        public decimal? buyPricePPPDiff { get; set; }

        public decimal? sellPricePPPDiff { get; set; }

        public decimal? buyDailyDiff { get; set; }

        public decimal? sellDailyDiff { get; set; }

        public decimal? totalDailyDiffPlain { get; set; }
    }
}

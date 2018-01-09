using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.Primary.Common.DTO
{
    public class DetailedPositions
    {
        public Dictionary<string, DetailedPositionItem> Items { get; set; }

        public decimal? instrumentMarketValue { get; set; }

        public decimal? instrumentInitialSize { get; set; }

        public decimal? instrumentFilledSize { get; set; }

        public decimal? instrumentCurrentSize { get; set; }
    }
}

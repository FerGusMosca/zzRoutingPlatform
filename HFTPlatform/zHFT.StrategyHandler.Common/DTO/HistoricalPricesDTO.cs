using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.StrategyHandler.Common.DTO
{
    public class HistoricalPricesDTO
    {
        public int ReqId { get; set; }

        public string Symbol { get; set; }

        public CandleInterval Interval { get; set; }

        public List<zHFT.Main.BusinessEntities.Market_Data.MarketData> MarketData { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.MarketClient.Common.DTO
{
    public class HistoricalPricesRequestDTO
    {
        #region Public Attributes

        public int ReqId { get; set; }

        public string Symbol { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public SecurityType SecurityType { get; set; }

        public CandleInterval Interval { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        #endregion
    }
}

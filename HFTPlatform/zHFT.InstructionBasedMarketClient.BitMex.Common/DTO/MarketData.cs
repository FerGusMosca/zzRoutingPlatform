using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO
{
    public class MarketData
    {
        #region Public Attributes

        public Security Security { get; set; }

        public decimal? OpeningPrice { get; set; }
        public double? ClosingPrice { get; set; }

        public double? TradingSessionHighPrice { get; set; }
        public double? TradingSessionLowPrice { get; set; }

        public decimal? TradeVolume { get; set; }
        public decimal? OpenInterest { get; set; }

        public double? Trade { get; set; }
        public DateTime? LastTradeDate { get; set; }
        public decimal? LastTradeSize { get; set; }

        public decimal? CashVolume { get; set; }
        public double? NominalVolume { get; set; }

        public double? SettlementPrice { get; set; }

        public double? BestBidPrice { get; set; }
        public long? BestBidSize { get; set; }

        public double? BestAskPrice { get; set; }
        public long? BestAskSize { get; set; }

        #endregion
    }
}

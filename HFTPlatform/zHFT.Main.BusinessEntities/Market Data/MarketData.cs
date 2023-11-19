using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Market_Data
{
    public class MarketData
    {
        #region Public Attributes

        #region Prices
        public double? OpeningPrice { get; set; }
        public double? ClosingPrice { get; set; }
        public double? SettlementPrice { get; set; }
        public double? TradingSessionHighPrice { get; set; }
        public double? TradingSessionLowPrice { get; set; }
        public double? TradingSessionVWAPPrice { get; set; }
        public double? Imbalance { get; set; }
        public double? TradeVolume { get; set; }
        public double? OpenInterest { get; set; }
        public double? CompositeUnderlyingPrice { get; set; }
        public double? MarginRate { get; set; }
        public double? MidPrice { get; set; }
        public double? SettleHighPrice { get; set; }
        public double? SettlPriorPrice { get; set; }
        public double? SessionHighBid { get; set; }
        public double? SessionLowOffer { get; set; }
        public double? EarlyPrices { get; set; }
        public double? AuctionClearingPrice { get; set; }
        public double? Trade { get; set; }

        public double? BestBidPrice { get; set; }
        public long? BestBidSize { get; set; }
        public string BestBidExch { get; set; }
        public double? BestAskPrice { get; set; }
        public long? BestAskSize { get; set; }
        public string BestAskExch { get; set; }

        public decimal? BestBidCashSize { get; set; }
        public decimal? BestAskCashSize { get; set; }

        public double? CashVolume { get; set; }
        public double? NominalVolume { get; set; }

        #endregion

        public UpdateAction MDUpdateAction { get; set; }
        public string Currency { get; set; }
        public virtual DateTime? MDEntryDate { get; set; }
        public virtual DateTime? MDLocalEntryDate { get; set; }
        public TickDirection TickDirection { get; set; }
        public double? MDTradeSize { get; set; }

        public SettlType SettlType { get; set; }
        public virtual DateTime? SettlDate { get; set; }

        public virtual DateTime? LastTradeDateTime { get; set; }

        public Security Security { get; set; }

        #endregion
        
        #region Public Methods

        public string GetDateTime()
        {
            if (MDEntryDate.HasValue)
                return MDEntryDate.Value.ToString("yyyy-MM-dd hh:mm:ss");
            else if (MDLocalEntryDate.HasValue)
                return MDLocalEntryDate.Value.ToString("yyyy-MM-dd hh:mm:ss");
            else
            {
                return "-";
            }

        }

        public DateTime? GetReferenceDateTime()
        {
            if (MDEntryDate.HasValue)
                return MDEntryDate.Value;
            else if (MDLocalEntryDate.HasValue)
                return MDLocalEntryDate.Value;
            else
            {
                throw new Exception($"Datetime cannot be null for market data");
            }

        }

        public override string ToString()
        {
            return string.Format(
                "DateTime={9} Symbol={0} Open={1} High={2} Low={3} Close={4} Trade={5} BestBidPx={6} BestAskPx={7} Volume={8}",
                Security.Symbol, OpeningPrice, TradingSessionHighPrice, TradingSessionLowPrice,
                ClosingPrice, Trade, BestBidPrice, BestAskPrice, TradeVolume,
                GetDateTime());
        }

        public bool BiggerGreendCandle(double refPrice)
        {
            //return ClosingPrice > refPrice && ClosingPrice > OpeningPrice;
            return ClosingPrice>=OpeningPrice && Trade > refPrice  ;
        }

      
        public bool LowerRedCandle(double refPrice)
        {
            //return ClosingPrice < refPrice && ClosingPrice < OpeningPrice;
            return ClosingPrice <= OpeningPrice &&  Trade < refPrice ;
        }


        public double? GetMidPrice()
        {

            if (BestAskPrice.HasValue && BestBidPrice.HasValue)
                return (BestAskPrice.Value + BestBidPrice.Value) / 2;
            else
            {
                return null;
            }
        }

        public DateTime? GetOrderingDate()
        {
            if (MDEntryDate.HasValue)
                return MDEntryDate.Value;
            else if (MDLocalEntryDate.HasValue)
                return MDLocalEntryDate.Value;
            else
            {
                return null;
            }


        }

        #endregion
    }
}

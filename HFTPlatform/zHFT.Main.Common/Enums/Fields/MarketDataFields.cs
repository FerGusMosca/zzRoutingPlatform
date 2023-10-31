using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class MarketDataFields : Fields
    {
        public static readonly Fields NoMDEntries = new MarketDataFields(2);

        #region Prices
        public static readonly MarketDataFields OpeningPrice = new MarketDataFields(3);
        public static readonly MarketDataFields ClosingPrice = new MarketDataFields(4);
        public static readonly MarketDataFields SettlementPrice = new MarketDataFields(5);
        public static readonly MarketDataFields TradingSessionHighPrice = new MarketDataFields(6);
        public static readonly MarketDataFields TradingSessionLowPrice = new MarketDataFields(7);
        public static readonly MarketDataFields TradingSessionVWAPPrice = new MarketDataFields(8);
        public static readonly MarketDataFields Imbalance = new MarketDataFields(9);
        public static readonly MarketDataFields TradeVolume = new MarketDataFields(10);
        public static readonly MarketDataFields OpenInterest = new MarketDataFields(11);
        public static readonly MarketDataFields CompositeUnderlyingPrice = new MarketDataFields(12);
        public static readonly MarketDataFields MarginRate = new MarketDataFields(13);
        public static readonly MarketDataFields MidPrice = new MarketDataFields(14);
        public static readonly MarketDataFields SettleHighPrice = new MarketDataFields(15);
        public static readonly MarketDataFields SettlPriorPrice = new MarketDataFields(16);
        public static readonly MarketDataFields SessionHighBid = new MarketDataFields(17);
        public static readonly MarketDataFields SessionLowOffer = new MarketDataFields(18);
        public static readonly MarketDataFields EarlyPrices = new MarketDataFields(19);
        public static readonly MarketDataFields AuctionClearingPrice = new MarketDataFields(20);
        public static readonly MarketDataFields Trade = new MarketDataFields(20);
        #endregion

        public static readonly MarketDataFields MDUpdateAction = new MarketDataFields(22);
        public static readonly MarketDataFields Currency = new MarketDataFields(23);
        public static readonly MarketDataFields MDEntryDate = new MarketDataFields(24);
        public static readonly MarketDataFields MDEntryTime = new MarketDataFields(25);
        public static readonly MarketDataFields TickDirection = new MarketDataFields(26);

        public static readonly MarketDataFields SettlType = new MarketDataFields(27);
        public static readonly MarketDataFields SettlDate = new MarketDataFields(28);

        public static readonly MarketDataFields Symbol = new MarketDataFields(29);
        public static readonly MarketDataFields SecurityType = new MarketDataFields(30);

        public static readonly MarketDataFields MDMkt = new MarketDataFields(31);

        public static readonly MarketDataFields BestBidPrice = new MarketDataFields(32);
        public static readonly MarketDataFields BestAskPrice = new MarketDataFields(33);
        public static readonly MarketDataFields BestAskSize = new MarketDataFields(34);
        public static readonly MarketDataFields BestBidSize = new MarketDataFields(35);
        public static readonly MarketDataFields BestAskExch = new MarketDataFields(35);
        public static readonly MarketDataFields BestBidExch = new MarketDataFields(36);
        public static readonly MarketDataFields MDTradeSize = new MarketDataFields(37);

        public static readonly MarketDataFields MDLocalEntryDate = new MarketDataFields(38);

        public static readonly MarketDataFields ReverseMarketData = new MarketDataFields(39);

        public static readonly MarketDataFields BestBidCashSize = new MarketDataFields(40);
        public static readonly MarketDataFields BestAskCashSize = new MarketDataFields(41);

        public static readonly MarketDataFields LastTradeDateTime = new MarketDataFields(42);
        
        public static readonly MarketDataFields BidEntryPx0 = new MarketDataFields(43);
        public static readonly MarketDataFields BidEntryPx1 = new MarketDataFields(44);
        public static readonly MarketDataFields BidEntryPx2 = new MarketDataFields(45);
        public static readonly MarketDataFields BidEntryPx3 = new MarketDataFields(46);
        public static readonly MarketDataFields BidEntryPx4 = new MarketDataFields(47);
        
        public static readonly MarketDataFields AskEntryPx0 = new MarketDataFields(48);
        public static readonly MarketDataFields AskEntryPx1 = new MarketDataFields(49);
        public static readonly MarketDataFields AskEntryPx2 = new MarketDataFields(50);
        public static readonly MarketDataFields AskEntryPx3 = new MarketDataFields(51);
        public static readonly MarketDataFields AskEntryPx4 = new MarketDataFields(52);
        
        public static readonly MarketDataFields BidEntrySize0 = new MarketDataFields(53);
        public static readonly MarketDataFields BidEntrySize1 = new MarketDataFields(54);
        public static readonly MarketDataFields BidEntrySize2 = new MarketDataFields(55);
        public static readonly MarketDataFields BidEntrySize3 = new MarketDataFields(56);
        public static readonly MarketDataFields BidEntrySize4 = new MarketDataFields(57);
        
        public static readonly MarketDataFields AskEntrySize0 = new MarketDataFields(58);
        public static readonly MarketDataFields AskEntrySize1 = new MarketDataFields(59);
        public static readonly MarketDataFields AskEntrySize2 = new MarketDataFields(60);
        public static readonly MarketDataFields AskEntrySize3 = new MarketDataFields(61);
        public static readonly MarketDataFields AskEntrySize4 = new MarketDataFields(62);
        
        protected MarketDataFields(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}

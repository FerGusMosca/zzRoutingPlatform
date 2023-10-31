using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers
{
    public class BitmexMarketDataWrapper :  Wrapper
    {
        #region Protected Attributes

        protected MarketData MarketData { get; set; }
        #endregion

        #region Constructor

        public BitmexMarketDataWrapper(MarketData marketData)
        {
            MarketData = marketData;
        }

        #endregion

        #region Wrapper Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

            if (MarketData == null)
                return MarketDataFields.NULL;

            if (mdField == MarketDataFields.Symbol)
                return MarketData.Security.Symbol;
            else if (mdField == MarketDataFields.SecurityType)
                return MarketData.Security.SecType;
            else if (mdField == MarketDataFields.Currency)
                return MarketData.Security.Currency;
            else if (mdField == MarketDataFields.MDMkt)
                return "BITMEX";

            else if (mdField == MarketDataFields.OpeningPrice)
                return MarketData.OpeningPrice;
            else if (mdField == MarketDataFields.ClosingPrice)
                return MarketData.ClosingPrice;
            else if (mdField == MarketDataFields.TradingSessionHighPrice)
                return MarketData.TradingSessionHighPrice;
            else if (mdField == MarketDataFields.TradingSessionLowPrice)
                return MarketData.TradingSessionLowPrice;
            else if (mdField == MarketDataFields.TradeVolume)
                return MarketData.TradeVolume;
            else if (mdField == MarketDataFields.OpenInterest)
                return MarketData.OpenInterest;
            else if (mdField == MarketDataFields.SettlType)
                return MarketData.SettlementPrice;
            else if (mdField == MarketDataFields.CompositeUnderlyingPrice)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.MidPrice)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.SessionHighBid)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.SessionLowOffer)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.EarlyPrices)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.Trade)
                return MarketData.Trade;
            else if (mdField == MarketDataFields.MDTradeSize)
                return MarketData.LastTradeSize;
            else if (mdField == MarketDataFields.BestBidPrice)
                return MarketData.BestBidPrice;
            else if (mdField == MarketDataFields.BestAskPrice)
                return MarketData.BestAskPrice;
            else if (mdField == MarketDataFields.BestBidSize)
                return MarketData.BestBidSize;
            else if (mdField == MarketDataFields.BestBidCashSize)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.BestAskSize)
                return MarketData.BestAskSize;
            else if (mdField == MarketDataFields.BestAskCashSize)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.BestBidExch)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.BestAskExch)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.MDEntryDate)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.MDEntryTime)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.MDLocalEntryDate)
                return MarketDataFields.NULL;
            else if (mdField == MarketDataFields.ReverseMarketData)
                return false;
            else if (mdField == MarketDataFields.LastTradeDateTime)
                return MarketData.LastTradeDate;

            return MarketDataFields.NULL;
        }


        public override Actions GetAction()
        {
            return Actions.MARKET_DATA;
        }

        #endregion
    }
}

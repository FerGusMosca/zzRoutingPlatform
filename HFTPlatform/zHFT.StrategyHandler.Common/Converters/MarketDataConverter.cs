using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Converters
{
    public class MarketDataConverter : ConverterBase
    {
        #region Private Methods
        private void RunMainValidations(Wrapper wrapper)
        {
            if (wrapper.GetAction() != Actions.MARKET_DATA)
                throw new Exception("Invalid action building market data");

        }

        protected void ValidateMarketData(Wrapper wrapper)
        {
            if (!ValidateField(wrapper, MarketDataFields.Symbol))
                throw new Exception("Missing symbol");
        
        
        }

        private Security BuildSecurity(Wrapper wrapper)
        {
            Security sec = new Security();
            sec.Symbol = (ValidateField(wrapper, MarketDataFields.Symbol) ? Convert.ToString(wrapper.GetField(MarketDataFields.Symbol)) : null);
            sec.SecType = (ValidateField(wrapper, MarketDataFields.SecurityType) ? (SecurityType)wrapper.GetField(MarketDataFields.SecurityType) : SecurityType.OTH);
            sec.Currency = (ValidateField(wrapper, MarketDataFields.Currency) ? Convert.ToString( wrapper.GetField(MarketDataFields.Currency)) : null);
            sec.Exchange = (ValidateField(wrapper, MarketDataFields.MDMkt) ? Convert.ToString(wrapper.GetField(MarketDataFields.MDMkt)) : null);
            sec.ReverseMarketData = (ValidateField(wrapper, MarketDataFields.ReverseMarketData) ? Convert.ToBoolean(wrapper.GetField(MarketDataFields.ReverseMarketData)) : false);
            return sec;
        }
        #endregion

        public OrderBook GetOrderBook(Wrapper wrapper, IConfiguration conf)
        {
            OrderBook ob= new OrderBook();
            
            List<OrderBookEntry> bids= new List<OrderBookEntry>();
            List<OrderBookEntry> asks = new List<OrderBookEntry>();
            
            ob.Security = BuildSecurity(wrapper);
            
            decimal? pxBid0= (ValidateField(wrapper, MarketDataFields.BidEntryPx0) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntryPx0)) : null);
            decimal? pxBid1= (ValidateField(wrapper, MarketDataFields.BidEntryPx1) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntryPx1)) : null);
            decimal? pxBid2= (ValidateField(wrapper, MarketDataFields.BidEntryPx2) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntryPx2)) : null);
            decimal? pxBid3= (ValidateField(wrapper, MarketDataFields.BidEntryPx3) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntryPx3)) : null);
            decimal? pxBid4= (ValidateField(wrapper, MarketDataFields.BidEntryPx4) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntryPx4)) : null);
            decimal? sizeBid0= (ValidateField(wrapper, MarketDataFields.BidEntrySize0) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntrySize0)) : null);
            decimal? sizeBid1= (ValidateField(wrapper, MarketDataFields.BidEntrySize1) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntrySize1)) : null);
            decimal? sizeBid2= (ValidateField(wrapper, MarketDataFields.BidEntrySize2) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntrySize2)) : null);
            decimal? sizeBid3= (ValidateField(wrapper, MarketDataFields.BidEntrySize3) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntrySize3)) : null);
            decimal? sizeBid4= (ValidateField(wrapper, MarketDataFields.BidEntrySize4) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.BidEntrySize4)) : null);

            decimal? pxAsk0= (ValidateField(wrapper, MarketDataFields.AskEntryPx0) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntryPx0)) : null);
            decimal? pxAsk1= (ValidateField(wrapper, MarketDataFields.AskEntryPx1) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntryPx1)) : null);
            decimal? pxAsk2= (ValidateField(wrapper, MarketDataFields.AskEntryPx2) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntryPx2)) : null);
            decimal? pxAsk3= (ValidateField(wrapper, MarketDataFields.AskEntryPx3) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntryPx3)) : null);
            decimal? pxAsk4= (ValidateField(wrapper, MarketDataFields.AskEntryPx4) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntryPx4)) : null);
            decimal? sizeAsk0= (ValidateField(wrapper, MarketDataFields.AskEntrySize0) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntrySize0)) : null);
            decimal? sizeAsk1= (ValidateField(wrapper, MarketDataFields.AskEntrySize1) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntrySize1)) : null);
            decimal? sizeAsk2= (ValidateField(wrapper, MarketDataFields.AskEntrySize2) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntrySize2)) : null);
            decimal? sizeAsk3= (ValidateField(wrapper, MarketDataFields.AskEntrySize3) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntrySize3)) : null);
            decimal? sizeAsk4= (ValidateField(wrapper, MarketDataFields.AskEntrySize4) ? (decimal?) Convert.ToDouble( wrapper.GetField(MarketDataFields.AskEntrySize4)) : null);

            if (pxBid0.HasValue && sizeBid0.HasValue)
                bids.Add(new OrderBookEntry() {Price = pxBid0.Value, Size = sizeBid0.Value});
            
            if (pxBid1.HasValue && sizeBid1.HasValue)
                bids.Add(new OrderBookEntry() {Price = pxBid1.Value, Size = sizeBid1.Value});
            
            
            if (pxBid2.HasValue && sizeBid2.HasValue)
                bids.Add(new OrderBookEntry() {Price = pxBid2.Value, Size = sizeBid2.Value});
            
            
            if (pxBid3.HasValue && sizeBid3.HasValue)
                bids.Add(new OrderBookEntry() {Price = pxBid3.Value, Size = sizeBid3.Value});
            
            
            if (pxBid4.HasValue && sizeBid4.HasValue)
                bids.Add(new OrderBookEntry() {Price = pxBid4.Value, Size = sizeBid4.Value});
            
            
            if (pxAsk0.HasValue && sizeAsk0.HasValue)
                asks.Add(new OrderBookEntry() {Price = pxAsk0.Value, Size = sizeAsk0.Value});
            
            if (pxAsk1.HasValue && sizeAsk1.HasValue)
                asks.Add(new OrderBookEntry() {Price = pxAsk1.Value, Size = sizeAsk1.Value});
            
            if (pxAsk2.HasValue && sizeAsk2.HasValue)
                asks.Add(new OrderBookEntry() {Price = pxAsk2.Value, Size = sizeAsk2.Value});
            
            if (pxAsk3.HasValue && sizeAsk3.HasValue)
                asks.Add(new OrderBookEntry() {Price = pxAsk3.Value, Size = sizeAsk3.Value});
            
            if (pxAsk4.HasValue && sizeAsk4.HasValue)
                asks.Add(new OrderBookEntry() {Price = pxAsk4.Value, Size = sizeAsk4.Value});


            ob.Bids = bids.ToArray();
            ob.Asks = asks.ToArray();

            return ob;
        }

        public MarketData GetMarketData(Wrapper wrapper, IConfiguration Config)
        {
            MarketData md = new MarketData();
            ValidateMarketData(wrapper);

            md.Security = BuildSecurity(wrapper);

            md.TradingSessionHighPrice = (ValidateField(wrapper, MarketDataFields.TradingSessionHighPrice) ? (double?) Convert.ToDouble( wrapper.GetField(MarketDataFields.TradingSessionHighPrice)) : null);
            md.TradingSessionLowPrice = (ValidateField(wrapper, MarketDataFields.TradingSessionLowPrice) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.TradingSessionLowPrice)) : null);
            md.OpenInterest = (ValidateField(wrapper, MarketDataFields.OpenInterest) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.OpenInterest)) : null);
            md.Imbalance = (ValidateField(wrapper, MarketDataFields.Imbalance) ? (double?) Convert.ToDouble ( wrapper.GetField(MarketDataFields.Imbalance)) : null);
            md.Trade = (ValidateField(wrapper, MarketDataFields.Trade) ?(double?) Convert.ToDouble( wrapper.GetField(MarketDataFields.Trade)) : null);
            md.OpeningPrice = (ValidateField(wrapper, MarketDataFields.OpeningPrice) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.OpeningPrice)) : null);
            md.ClosingPrice = (ValidateField(wrapper, MarketDataFields.ClosingPrice) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.ClosingPrice)) : null);
            md.BestBidPrice = (ValidateField(wrapper, MarketDataFields.BestBidPrice) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.BestBidPrice)) : null);
            md.BestAskPrice = (ValidateField(wrapper, MarketDataFields.BestAskPrice) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.BestAskPrice)) : null);
            md.BestBidSize = (ValidateField(wrapper, MarketDataFields.BestBidSize) ? (long?) Convert.ToInt64(wrapper.GetField(MarketDataFields.BestBidSize)) : null);
            md.BestAskSize = (ValidateField(wrapper, MarketDataFields.BestAskSize) ? (long?) Convert.ToInt64(wrapper.GetField(MarketDataFields.BestAskSize)) : null);
            md.TradeVolume = (ValidateField(wrapper, MarketDataFields.TradeVolume) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.TradeVolume)) : null);
            md.MDTradeSize = (ValidateField(wrapper, MarketDataFields.MDTradeSize) ? (double?) Convert.ToDouble(wrapper.GetField(MarketDataFields.MDTradeSize)) : null);
            md.BestAskExch = (ValidateField(wrapper, MarketDataFields.BestAskExch) ? Convert.ToString( wrapper.GetField(MarketDataFields.BestAskExch)) : null);
            md.BestBidExch = (ValidateField(wrapper, MarketDataFields.BestBidExch) ? Convert.ToString( wrapper.GetField(MarketDataFields.BestBidExch)) : null);
            md.SettlType = (ValidateField(wrapper, MarketDataFields.SettlType) ?(SettlType) wrapper.GetField(MarketDataFields.SettlType) : SettlType.Regular);
            md.MDEntryDate = (ValidateField(wrapper, MarketDataFields.MDEntryDate) ? (DateTime?) wrapper.GetField(MarketDataFields.MDEntryDate) : null);
            md.MDLocalEntryDate = (ValidateField(wrapper, MarketDataFields.MDLocalEntryDate) ? (DateTime?)wrapper.GetField(MarketDataFields.MDLocalEntryDate) : null);

            md.Currency = (ValidateField(wrapper, MarketDataFields.Currency) ? Convert.ToString(wrapper.GetField(MarketDataFields.Currency)) : null);
            
            md.BestBidCashSize = (ValidateField(wrapper, MarketDataFields.BestBidCashSize) ? (decimal?)Convert.ToDecimal(wrapper.GetField(MarketDataFields.BestBidCashSize)) : null);
            md.BestAskCashSize = (ValidateField(wrapper, MarketDataFields.BestAskCashSize) ? (decimal?)Convert.ToDecimal(wrapper.GetField(MarketDataFields.BestAskCashSize)) : null);

            md.LastTradeDateTime = (ValidateField(wrapper, MarketDataFields.LastTradeDateTime) ? (DateTime?)wrapper.GetField(MarketDataFields.LastTradeDateTime) : null);

            if (md.MDEntryDate.HasValue && !md.MDLocalEntryDate.HasValue)
                md.MDLocalEntryDate = md.MDLocalEntryDate;
            else if (md.MDLocalEntryDate.HasValue && !md.MDEntryDate.HasValue)
                md.MDEntryDate = md.MDLocalEntryDate;
            
            
            return md;
        
        }
    }
}

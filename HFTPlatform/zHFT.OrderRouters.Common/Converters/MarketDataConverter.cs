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

namespace zHFT.OrderRouters.Common.Converters
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
            sec.Currency = (ValidateField(wrapper, MarketDataFields.Currency) ? Convert.ToString(wrapper.GetField(MarketDataFields.Currency)) : null);
            sec.Exchange = (ValidateField(wrapper, MarketDataFields.MDMkt) ? Convert.ToString(wrapper.GetField(MarketDataFields.MDMkt)) : null);

            return sec;
        }

        #endregion

        public MarketData GetMarketData(Wrapper wrapper, IConfiguration Config)
        {
            MarketData md = new MarketData();
            ValidateMarketData(wrapper);

            md.Security = BuildSecurity(wrapper);

            md.TradingSessionHighPrice = (ValidateField(wrapper, MarketDataFields.TradingSessionHighPrice) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.TradingSessionHighPrice)) : null);
            md.TradingSessionLowPrice = (ValidateField(wrapper, MarketDataFields.TradingSessionLowPrice) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.TradingSessionLowPrice)) : null);
            md.OpenInterest = (ValidateField(wrapper, MarketDataFields.OpenInterest) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.OpenInterest)) : null);
            md.Imbalance = (ValidateField(wrapper, MarketDataFields.Imbalance) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.Imbalance)) : null);
            md.Trade = (ValidateField(wrapper, MarketDataFields.Trade) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.Trade)) : null);
            md.OpeningPrice = (ValidateField(wrapper, MarketDataFields.OpeningPrice) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.OpeningPrice)) : null);
            md.ClosingPrice = (ValidateField(wrapper, MarketDataFields.ClosingPrice) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.ClosingPrice)) : null);
            md.BestBidPrice = (ValidateField(wrapper, MarketDataFields.BestBidPrice) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.BestBidPrice)) : null);
            md.BestAskPrice = (ValidateField(wrapper, MarketDataFields.BestAskPrice) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.BestAskPrice)) : null);
            md.BestBidSize = (ValidateField(wrapper, MarketDataFields.BestBidSize) ? (long?)Convert.ToInt64(wrapper.GetField(MarketDataFields.BestBidSize)) : null);
            md.BestAskSize = (ValidateField(wrapper, MarketDataFields.BestAskSize) ? (long?)Convert.ToInt64(wrapper.GetField(MarketDataFields.BestAskSize)) : null);
            md.BestBidCashSize = (ValidateField(wrapper, MarketDataFields.BestBidCashSize) ? (decimal?)Convert.ToDecimal(wrapper.GetField(MarketDataFields.BestBidCashSize)) : null);
            md.BestAskCashSize = (ValidateField(wrapper, MarketDataFields.BestAskCashSize) ? (decimal?)Convert.ToDecimal(wrapper.GetField(MarketDataFields.BestAskCashSize)) : null);

            md.TradeVolume = (ValidateField(wrapper, MarketDataFields.TradeVolume) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.TradeVolume)) : null);
            md.MDTradeSize = (ValidateField(wrapper, MarketDataFields.MDTradeSize) ? (double?)Convert.ToDouble(wrapper.GetField(MarketDataFields.MDTradeSize)) : null);
            md.BestAskExch = (ValidateField(wrapper, MarketDataFields.BestAskExch) ? Convert.ToString(wrapper.GetField(MarketDataFields.BestAskExch)) : null);
            md.BestBidExch = (ValidateField(wrapper, MarketDataFields.BestBidExch) ? Convert.ToString(wrapper.GetField(MarketDataFields.BestBidExch)) : null);

            return md;
        }
    }
}

using System;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class HistoricalPricesRequestWrapper:Wrapper
    {
        #region Constructors

        public HistoricalPricesRequestWrapper(int reqId ,string pSymbol,DateTime? pFrom,DateTime? pTo,CandleInterval pInterval)
        {
            MdReqId = reqId;
            Symbol = pSymbol;
            From = pFrom;
            To = pTo;
            TimeInterval = pInterval;

        }

        public HistoricalPricesRequestWrapper(int reqId, string pSymbol, DateTime? pFrom, DateTime? pTo,
            CandleInterval pInterval,string pCurrency,SecurityType pSecurityType)
        {
            MdReqId = reqId;
            Symbol = pSymbol;
            From = pFrom;
            To = pTo;
            TimeInterval = pInterval;
            Currency = pCurrency;
            SecurityType = pSecurityType;


        }

        #endregion
        
        #region Public Attributes
        
        protected int MdReqId { get; set; }
        protected string Symbol { get; set; }
        
        public DateTime? From { get; set; }
        
        public DateTime? To { get; set; }
        
        public CandleInterval TimeInterval { get; set; }
        
        public  string Currency { get; set; }
        
        public  SecurityType SecurityType { get; set; }
        
        #endregion
        
        public override object GetField(Main.Common.Enums.Fields field)
        {
            HistoricalPricesRequestFields sField = (HistoricalPricesRequestFields)field;


            if (sField == HistoricalPricesRequestFields.Symbol)
                return Symbol;
            else if (sField == HistoricalPricesRequestFields.From)
                return From;
            else if (sField == HistoricalPricesRequestFields.To)
                return To;
            else if (sField == HistoricalPricesRequestFields.Interval)
                return TimeInterval;
            else if (sField == HistoricalPricesRequestFields.MDReqId)
                return MdReqId;
            else if (sField == HistoricalPricesRequestFields.Currency)
                return Currency;
            else if (sField == HistoricalPricesRequestFields.SecurityType)
                return SecurityType;
          
            return HistoricalPricesRequestFields.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.HISTORICAL_PRICES_REQUEST;
        }
    }
}
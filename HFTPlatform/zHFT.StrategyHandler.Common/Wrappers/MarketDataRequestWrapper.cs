using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class MarketDataRequestWrapper:Wrapper
    {
        #region Private Consts

        private string _EXCHANGE_SEPARATOR_FIELD = ".";

        #endregion

        #region Protected Attributes

        protected long MdReqId { get; set; }

        protected Security Security { get; set; }

        protected string QuoteSymbol { get; set; }

        protected SubscriptionRequestType SubscriptionRequestType { get; set; }

        #endregion

        #region Constructors

        public MarketDataRequestWrapper(Security pSecurity, SubscriptionRequestType pSubscriptionRequestType,string pQuoteSymbol=null)
        {
            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);
            MdReqId = Convert.ToInt64(elapsed.TotalMilliseconds) ;
            Security = pSecurity;
            SubscriptionRequestType = pSubscriptionRequestType;
            QuoteSymbol = pQuoteSymbol;
        }

        public MarketDataRequestWrapper(int pMdReqId,Security pSecurity, SubscriptionRequestType pSubscriptionRequestType)
        {
            MdReqId = pMdReqId;
            Security = pSecurity;
            SubscriptionRequestType = pSubscriptionRequestType;
        }

        #endregion

        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataRequestField mdrField = (MarketDataRequestField)field;

            if (Security == null)
                return MarketDataRequestField.NULL;

            if (mdrField == MarketDataRequestField.Symbol)
                return Security.Symbol;
            if (mdrField == MarketDataRequestField.Exchange)
            {
                if (!string.IsNullOrEmpty(Security.Exchange))
                    return Security.Exchange;
                else if (Security.Symbol.Contains(_EXCHANGE_SEPARATOR_FIELD))
                    return Security.Symbol.Split(new string[] { _EXCHANGE_SEPARATOR_FIELD }, StringSplitOptions.RemoveEmptyEntries)[1];
                else
                    return null;
            }
            if (mdrField == MarketDataRequestField.SecurityType)
                return Security.SecType;
            if (mdrField == MarketDataRequestField.Currency)
                return Security.Currency;
            if (mdrField == MarketDataRequestField.QuoteSymbol)
                return QuoteSymbol;
            if (mdrField == MarketDataRequestField.MDReqId)
                return MdReqId;
            if (mdrField == MarketDataRequestField.SettlType)
                return Security.MarketData != null ? Security.MarketData.SettlType : SettlType.Tplus2;
            if (mdrField == MarketDataRequestField.SubscriptionRequestType)
                return SubscriptionRequestType;
            else
                return MarketDataRequestField.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.MARKET_DATA_REQUEST;
        }

        #endregion
    }
}

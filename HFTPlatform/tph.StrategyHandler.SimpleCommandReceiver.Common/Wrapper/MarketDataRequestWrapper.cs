using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper
{
    public class MarketDataRequestWrapper: zHFT.Main.Common.Wrappers.Wrapper
    {
        #region Protected Attributes
        
        protected int MdReqId { get; set; }

        protected Security Security { get; set; }

        protected SubscriptionRequestType SubscriptionRequestType { get; set; }
        
        protected MarketDepth MarketDepth { get; set; }

        #endregion
        
        #region Constructors

        public MarketDataRequestWrapper(int pReqId, Security pSecurity, SubscriptionRequestType pType,
                                        MarketDepth pDetph)
        {
            MdReqId = pReqId;
            Security = pSecurity;
            SubscriptionRequestType = pType;
            MarketDepth = pDetph;
        }

        #endregion
        
        #region Public Methods
        
        public override object GetField(Fields field)
        {
            MarketDataRequestField mdrField = (MarketDataRequestField)field;

            if (Security == null)
                return MarketDataRequestField.NULL;

            if (mdrField == MarketDataRequestField.Symbol)
                return Security.Symbol;
            if (mdrField == MarketDataRequestField.Exchange)
            {
                return null;
            }
            if (mdrField == MarketDataRequestField.SecurityType)
                return Security.SecType;
            if (mdrField == MarketDataRequestField.Currency)
                return Security.Currency;
            if (mdrField == MarketDataRequestField.MDReqId)
                return MdReqId;
            if (mdrField == MarketDataRequestField.SubscriptionRequestType)
                return SubscriptionRequestType;
            if (mdrField == MarketDataRequestField.MarketDepth)
                return MarketDepth;
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
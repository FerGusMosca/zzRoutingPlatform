using System.Collections.Generic;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Common.Wrappers
{
    public class HistoricalPricesWrapper:Wrapper
    {
        
        #region Constructors

        public HistoricalPricesWrapper(int pReqId,Security pSecurity,CandleInterval pInterval,List<Wrapper> candlesWrapper)
        {
            CandlesWrapper = candlesWrapper;
            Security = pSecurity;
            CandleInterval = pInterval;
            ReqId= pReqId;
        }

        #endregion
        
        #region Protected Attributes

        protected int ReqId { get; set; }
        
        
        protected Security Security { get; set; }
        protected List<Wrapper> CandlesWrapper { get; set; }

        protected CandleInterval CandleInterval { get; set; }

        #endregion

        #region Public Methods
        public override object GetField(Fields field)
        {
            
            HistoricalPricesFields hpField = (HistoricalPricesFields)field;

            if (Security == null)
                return HistoricalPricesFields.NULL;

            if (hpField == HistoricalPricesFields.Security)
                return Security;
            else if (hpField == HistoricalPricesFields.Candles)
                return CandlesWrapper;
            else if (hpField == HistoricalPricesFields.Interval)
                return CandleInterval;
            else if (hpField == HistoricalPricesFields.RequestId)
                return ReqId;
            else if (hpField == HistoricalPricesFields.NULL)//default
                return CandlesWrapper;
            else
                return CandlesWrapper;
        }

        public override Actions GetAction()
        {
            return Actions.HISTORICAL_PRICES;
        }
        
        #endregion
    }
}
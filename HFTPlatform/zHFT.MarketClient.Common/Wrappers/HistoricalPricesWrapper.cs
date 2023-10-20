using System.Collections.Generic;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Common.Wrappers
{
    public class HistoricalPricesWrapper:Wrapper
    {
        
        #region Constructors

        public HistoricalPricesWrapper(Security pSecurity,List<Wrapper> candlesWrapper)
        {
            CandlesWrapper = candlesWrapper;
            Security = pSecurity;
        }

        #endregion
        
        #region Protected Attributes
        
        
        protected Security Security { get; set; }
        protected List<Wrapper> CandlesWrapper { get; set; }
        
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
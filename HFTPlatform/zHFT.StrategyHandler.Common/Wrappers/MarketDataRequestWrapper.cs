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
        #region Protected Attributes

        protected Security Security { get; set; }

        #endregion


        #region Constructors

        public MarketDataRequestWrapper(Security pSecurity)
        {
            Security = pSecurity;
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
                return Security.Exchange;
            if (mdrField == MarketDataRequestField.SecurityType)
                return Security.SecType;
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

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
    public class MarketDataRequestBulkWrapper : Wrapper
    {

        #region Protected Attributes

        protected List<Security> Securities { get; set; }

        protected SubscriptionRequestType SubscriptionRequestType { get; set; }

        protected MarketDepth MarketDepth { get; set; }

        protected SettlType SettlType { get; set; }

        #endregion

        #region Constructors

        public MarketDataRequestBulkWrapper(List<Security> pSecurities, SubscriptionRequestType pSubscriptionRequestType,
                                            MarketDepth pMarketDepth= MarketDepth.TopOfBook, SettlType pSettlType=SettlType.Tplus2)
        {
            Securities = pSecurities;
            SubscriptionRequestType = pSubscriptionRequestType;
            MarketDepth = pMarketDepth;
            SettlType = pSettlType;
        }


        #endregion

        #region Public Overriden Methods

        public override Actions GetAction()
        {
            return Actions.MARKET_DATA_REQUEST_BULK;
        }

        public override object GetField(Fields field)
        {
            MarketDataRequestBulkField mdrbField = (MarketDataRequestBulkField)field;

            if (Securities == null)
                return MarketDataRequestField.NULL;

            if (mdrbField == MarketDataRequestBulkField.Securities)
                return Securities.ToArray();
            else if (mdrbField == MarketDataRequestBulkField.SubscriptionRequestType)
                return SubscriptionRequestType;
            else if (mdrbField == MarketDataRequestBulkField.MarketDepth)
                return MarketDepth;
            else if (mdrbField == MarketDataRequestBulkField.SettlType)
                return SettlType;
            else
                return MarketDataRequestField.NULL;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Market_Data
{
    public class MarketDataRequestBulk
    {
        #region Public Attributes

        public int ReqId { get; set; }

        public Security[] Securities { get; set; }

        public SubscriptionRequestType SubscriptionRequestType { get; set; }

        public SettlType SettlType { get; set; }

        public MarketDepth MarketDepth { get; set; }

        #endregion
    }
}

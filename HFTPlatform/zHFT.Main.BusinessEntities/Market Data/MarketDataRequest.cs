using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Market_Data
{
    public class MarketDataRequest
    {
        #region Public Attributes

        public int ReqId { get; set; }

        public Security Security { get; set; }

        public SubscriptionRequestType SubscriptionRequestType { get; set; }


        #endregion
    }
}

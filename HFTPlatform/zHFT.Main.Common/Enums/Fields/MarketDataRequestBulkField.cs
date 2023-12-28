using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class MarketDataRequestBulkField : Fields
    {
        public static readonly MarketDataRequestBulkField Securities = new MarketDataRequestBulkField(2);
        public static readonly MarketDataRequestBulkField SubscriptionRequestType = new MarketDataRequestBulkField(3);
        public static readonly MarketDataRequestBulkField MarketDepth = new MarketDataRequestBulkField(4);
        public static readonly MarketDataRequestBulkField SettlType = new MarketDataRequestBulkField(5);
        //


        protected MarketDataRequestBulkField(int pInternalValue)
          : base(pInternalValue)
        {

        }
    }
}

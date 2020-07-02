using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Enums
{
    public class MarketDataRequestField : Fields
    {
        public static readonly MarketDataRequestField Symbol = new MarketDataRequestField(2);
        public static readonly MarketDataRequestField Exchange = new MarketDataRequestField(3);
        public static readonly MarketDataRequestField SecurityType = new MarketDataRequestField(4);
        public static readonly MarketDataRequestField SubscriptionRequestType = new MarketDataRequestField(5);
        public static readonly MarketDataRequestField Currency = new MarketDataRequestField(6);
        public static readonly MarketDataRequestField MDReqId = new MarketDataRequestField(7);
        public static readonly MarketDataRequestField SettlType = new MarketDataRequestField(8);
        public static readonly MarketDataRequestField QuoteSymbol = new MarketDataRequestField(9);


        protected MarketDataRequestField(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}

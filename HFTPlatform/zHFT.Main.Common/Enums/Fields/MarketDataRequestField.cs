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



        protected MarketDataRequestField(int pInternalValue)
            : base(pInternalValue)
        {

        }
    }
}

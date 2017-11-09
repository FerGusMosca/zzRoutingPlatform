using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;


namespace zHFT.SecurityListMarketClient.Primary.Common.Converters
{
    public class MarketDataRequestConverter
    {
        public static MarketDataRequest GetMarketDataRequest(Wrapper wrapper,string marketPrefixCode,string marketClearingId)
        {
            MarketDataRequest mdr = new MarketDataRequest();

            mdr.Symbol = SecurityConverter.GetCleanSymbolFromFullSymbol((string)wrapper.GetField(MarketDataRequestField.Symbol));
            mdr.Exchange = (string)wrapper.GetField(MarketDataRequestField.Exchange);

            mdr.Symbol = marketPrefixCode + " - " + mdr.Symbol + " - " + marketClearingId;

            return mdr;

        }
    }
}

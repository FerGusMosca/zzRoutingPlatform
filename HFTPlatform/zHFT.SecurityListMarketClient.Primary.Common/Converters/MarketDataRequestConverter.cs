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
        public static MarketDataRequest GetMarketDataRequest(Wrapper wrapper,string marketPrefixCode,string marketClearingId,SecurityType secType)
        {
            MarketDataRequest mdr = new MarketDataRequest();

            if (secType == SecurityType.CS || secType == SecurityType.TB)
            {
                mdr.Symbol = SecurityConverter.GetCleanSymbolFromFullSymbol((string)wrapper.GetField(MarketDataRequestField.Symbol));
            }
            else if (secType == SecurityType.OPT)
            {
                mdr.Symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            }
            else
                throw new Exception(string.Format("Security Type translation not yet implemented: {0}", secType.ToString()));

            mdr.Exchange = (string)wrapper.GetField(MarketDataRequestField.Exchange);

            if(!string.IsNullOrEmpty(marketPrefixCode) &&  !string.IsNullOrEmpty(marketClearingId))
                mdr.Symbol = marketPrefixCode + " - " + mdr.Symbol + " - " + marketClearingId;

            return mdr;

        }
    }
}

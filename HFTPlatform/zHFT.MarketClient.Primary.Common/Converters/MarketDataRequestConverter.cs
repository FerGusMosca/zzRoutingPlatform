using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;


namespace zHFT.MarketClient.Primary.Common.Converters
{
    public class MarketDataRequestConverter
    {
        public static MarketDataRequest GetMarketDataRequest(Wrapper wrapper)
        {
            string exchange = (string)wrapper.GetField(MarketDataRequestField.Exchange);
            string exchangePrefixCode = ExchangeConverter.GetMarketPrefixCode(exchange);
            zHFT.Main.Common.Enums.SecurityType secType = (zHFT.Main.Common.Enums.SecurityType)wrapper.GetField(MarketDataRequestField.SecurityType);
            string marketClearingID = ExchangeConverter.GetMarketClearingID(secType, exchange);

            MarketDataRequest mdr = new MarketDataRequest();
            mdr.Security = new Security();

            if (secType == SecurityType.CS || secType == SecurityType.TB)
            {
                mdr.Security.Symbol = SymbolConverter.GetCleanSymbolFromFullSymbol((string)wrapper.GetField(MarketDataRequestField.Symbol));
            }
            else if (secType == SecurityType.OPT)
            {
                mdr.Security.Symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            }
            else
                throw new Exception(string.Format("Security Type translation not yet implemented: {0}", secType.ToString()));

            mdr.Security.Exchange = (string)wrapper.GetField(MarketDataRequestField.Exchange);
            mdr.Security.Currency = (string)wrapper.GetField(MarketDataRequestField.Currency);
            mdr.Security.SecType = (SecurityType)wrapper.GetField(MarketDataRequestField.SecurityType);
            mdr.SubscriptionRequestType = (SubscriptionRequestType)wrapper.GetField(MarketDataRequestField.SubscriptionRequestType);

            if (!string.IsNullOrEmpty(exchangePrefixCode) && !string.IsNullOrEmpty(marketClearingID))
                mdr.Security.Symbol = exchangePrefixCode + " - " + mdr.Security.Symbol + " - " + marketClearingID;

            return mdr;

        }
    }
}

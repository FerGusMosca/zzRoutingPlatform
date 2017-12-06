using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Converters
{
    public class MarketDataRequestConverter
    {
        public static MarketDataRequest GetMarketDataRequest(Wrapper wrapper)
        {
            MarketDataRequest mdr = new MarketDataRequest();
            mdr.Security = new Security();

            //No hay mayores problemas con el IB pues se considera el estandar de la App
            mdr.Security.Symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            mdr.Security.Exchange = (string)wrapper.GetField(MarketDataRequestField.Exchange);
            mdr.Security.Currency = (string)wrapper.GetField(MarketDataRequestField.Currency);
            mdr.Security.SecType = (SecurityType)wrapper.GetField(MarketDataRequestField.SecurityType);
            mdr.SubscriptionRequestType = (SubscriptionRequestType)wrapper.GetField(MarketDataRequestField.SubscriptionRequestType);

            return mdr;

        }
    }
}

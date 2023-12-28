using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Converters
{
    public class MarketDataRequestConverter: ConverterBase
    {

        public static MarketDataRequestBulk GetMarketDataRequestBulk(Wrapper wrapper)
        {
            MarketDataRequestBulk mdrb = new MarketDataRequestBulk();


            Security[] securities  = (Security[])wrapper.GetField(MarketDataRequestBulkField.Securities);
            if (securities != null)
                mdrb.Securities = securities;
            else
                throw new Exception($"Missing mandatory field Securities @MarketDataRequestBulkRequest ");




            SubscriptionRequestType srt= (SubscriptionRequestType)wrapper.GetField(MarketDataRequestBulkField.SubscriptionRequestType);
            if (srt != null)
                mdrb.SubscriptionRequestType = srt;
            else
                throw new Exception($"Missing mandatory field SubscriptionRequestType @MarketDataRequestBulkRequest");




            if (ValidateField(wrapper, MarketDataRequestBulkField.MarketDepth))
                mdrb.MarketDepth = (MarketDepth)wrapper.GetField(MarketDataRequestBulkField.MarketDepth);
            else
                mdrb.MarketDepth = MarketDepth.TopOfBook;


            mdrb.SettlType = (SettlType)wrapper.GetField(MarketDataRequestBulkField.SettlType);


            return mdrb;


        }


        public static  MarketDataRequest GetMarketDataRequest(Wrapper wrapper)
        {
            MarketDataRequest mdr = new MarketDataRequest();
            mdr.Security = new Security();

            //No hay mayores problemas con el IB pues se considera el estandar de la App
            mdr.Security.Symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
            mdr.Security.Exchange = (string)wrapper.GetField(MarketDataRequestField.Exchange);
            mdr.Security.Currency = (string)wrapper.GetField(MarketDataRequestField.Currency);
            mdr.Security.SecType = (SecurityType)wrapper.GetField(MarketDataRequestField.SecurityType);
            mdr.SubscriptionRequestType = (SubscriptionRequestType)wrapper.GetField(MarketDataRequestField.SubscriptionRequestType);
            mdr.SettlType = (SettlType)wrapper.GetField(MarketDataRequestField.SettlType);

            if (ValidateField(wrapper, MarketDataRequestField.MarketDepth))
                mdr.MarketDepth = (MarketDepth) wrapper.GetField(MarketDataRequestField.MarketDepth);
            else
                mdr.MarketDepth = MarketDepth.TopOfBook;
            
            return mdr;

        }
    }
}

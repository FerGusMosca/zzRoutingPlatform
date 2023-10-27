using IBApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace zHFT.MarketClient.IB.Common.Converters
{
    public class SecurityConverter
    {
        #region Private Static Consts

        private static string _STOCK = "STK";
        private static string _OPTIONS = "OPT";
        private static string _FUTURES = "FUT";
        private static string _INDEX = "IND";
        private static string _CASH = "CASH";

        #endregion

        #region Public Static Methods

        //We convert IB security type codes into Generic Type Codes
        public static SecurityType GetSecurityTypeFromIBCode(string ibSecurityTypeCode)
        {
            if (ibSecurityTypeCode == SecurityConverter._STOCK)
                return SecurityType.CS;
            else if (ibSecurityTypeCode == _OPTIONS)
                return SecurityType.OPT;
            else if (ibSecurityTypeCode == _FUTURES)
                return SecurityType.FUT;
            else if (ibSecurityTypeCode == _INDEX)
                return SecurityType.IND;
            else if (ibSecurityTypeCode == _CASH)
                return SecurityType.CASH;
            else
                return SecurityType.OTH;

        }

        public static void AssignValueBasedOnField(Security security,int field, double value)
        {

            if (security == null)
                throw new Exception("Cannot assign value to a null security!!");


            if (security.MarketData == null)
                throw new Exception("Cannot assign value to a null market data!!");

            if (field == TickType.HIGH)
                security.MarketData.TradingSessionHighPrice = value;
            else if (field == TickType.LOW)
                security.MarketData.TradingSessionLowPrice = value;
            else if (field == TickType.OPEN_INTEREST)
                security.MarketData.OpenInterest = value;
            else if (field == TickType.REGULATORY_IMBALANCE)
                security.MarketData.Imbalance = value;
            else if (field == TickType.LAST)
                security.MarketData.Trade = value;
            else if (field == TickType.OPEN)
                security.MarketData.OpeningPrice = value;
            else if (field == TickType.CLOSE)
                security.MarketData.ClosingPrice = value;
            else if (field == TickType.BID && value>0)
                security.MarketData.BestBidPrice = value;
            else if (field == TickType.ASK && value>0)
                security.MarketData.BestAskPrice = value;
            else if (field == TickType.HALTED)
                security.Halted  = (Halted) value;

        
        }

        public static void AssignValueBasedOnField(Security security, int field, int value)
        {
            if (security == null)
                throw new Exception("Cannot assign value to a null security!!");

            if (security.MarketData == null)
                throw new Exception("Cannot assign value to a null market data!!");

            if (field == TickType.VOLUME)
                security.MarketData.TradeVolume = Convert.ToDouble(value);
            else if (field == TickType.LAST_SIZE)
                security.MarketData.MDTradeSize = Convert.ToDouble(value);
            else if (field == TickType.ASK_SIZE)
                security.MarketData.BestAskSize = Convert.ToInt64(value);
            else if (field == TickType.BID_SIZE)
                security.MarketData.BestBidSize = Convert.ToInt64(value);

        }

        public static void AssignValueBasedOnField(Security security, int field, string value)
        {
            if (security == null)
                throw new Exception("Cannot assign value to a null security!!");

            if (security.MarketData == null)
                throw new Exception("Cannot assign value to a null market data!!");

            if (field == TickType.BID_EXCH)
                security.MarketData.BestBidExch = value;
            else if (field == TickType.ASK_EXCH)
                security.MarketData.BestAskExch = value;
            else if (field == TickType.LAST_TIMESTAMP)
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                security.MarketData.LastTradeDateTime = epoch.AddSeconds(Convert.ToDouble(value));
            }

        }

        public static List<Security> BuildOptionChainSecurities(int reqId, string underSymbol,string currency, string exchange, int underlyingConId, string tradingClass,
            string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            List<Security> optionChain = new List<Security>();
            foreach (string exp in expirations)
            {

                foreach (double strike in strikes)
                {
                    
                    Security secCall = new Security();
                    secCall.StrikePrice = strike;
                    secCall.SecType = SecurityType.OPT;
                    secCall.MaturityDate = (DateTime?)DateTime.ParseExact(exp, "yyyyMMdd", CultureInfo.InvariantCulture);
                    secCall.MaturityMonthYear = exp.Substring(0, 6);
                    secCall.Currency = currency;
                    secCall.PutOrCall = PutOrCall.Call;
                    secCall.StrikeMultiplier = Convert.ToInt32(multiplier);
                    secCall.Exchange = exchange;
                    secCall.AltIntSymbol = underlyingConId.ToString();
                    secCall.SymbolSfx = tradingClass;
                    secCall.Symbol = secCall.BuildOptionSymbol();

                    optionChain.Add(secCall);
                    Security secPut = secCall.CallToPut();
                    optionChain.Add(secPut);
                }
            }

            return optionChain;
        }

        #endregion
    }
}

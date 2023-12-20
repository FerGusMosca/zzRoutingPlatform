using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;


namespace zHFT.Main.Common.Util
{
    public class FullSymbolManager
    {

        #region Private Static Consts

        private static string _MISSING_KEY_WILDCARD = "*";

        #endregion


        #region Public Static Methods

        public static SecurityType GetSecurityTypeFromStr(string secType)
        {
            if (string.IsNullOrEmpty(secType))
                return SecurityType.OTH;

            if (secType.ToUpper() == SecurityType.CASH.ToString())
                return SecurityType.CASH;
            else if (secType.ToUpper() == SecurityType.CS.ToString())
                return SecurityType.CS;
            else if (secType.ToUpper() == SecurityType.FUT.ToString())
                return SecurityType.FUT;
            else if (secType.ToUpper() == SecurityType.IND.ToString())
                return SecurityType.IND;
            else if (secType.ToUpper() == SecurityType.OPT.ToString())
                return SecurityType.OPT;
            else if (secType.ToUpper() == SecurityType.TB.ToString())
                return SecurityType.TB;
            else if (secType.ToUpper() == SecurityType.TBOND.ToString())
                return SecurityType.TBOND;
            else if (secType.ToUpper() == SecurityType.CMDTY.ToString())
                return SecurityType.CMDTY;
            else if (secType.ToUpper() == SecurityType.OTH.ToString())
                return SecurityType.OTH;
            else
                return SecurityType.OTH;
        }

        public static string BuildFullSymbol(string symbol, string exchange, SecurityType? securityType)
        {
            string fullSymbol = "";

            //We appply the full symbol convention
            if (!string.IsNullOrEmpty(exchange) && !securityType.HasValue)
                fullSymbol = symbol;
            else if (string.IsNullOrEmpty(exchange) && securityType.HasValue)
                fullSymbol = $"{symbol}.{_MISSING_KEY_WILDCARD}.{securityType.Value.ToString()}";//only sec type
            else if (!string.IsNullOrEmpty(exchange) && !securityType.HasValue)
                fullSymbol = $"{symbol}.{exchange}.{_MISSING_KEY_WILDCARD}";//only exchange
            else if (!string.IsNullOrEmpty(exchange) && securityType.HasValue)
                fullSymbol = $"{symbol}.{exchange}.{securityType.Value.ToString()}";//exchange and sec type
            else
                fullSymbol = symbol;//only the symbol


            return fullSymbol;

        }
     

        public static string GetCleanSymbol(string fullSymbol)
        {
            string[] fields = fullSymbol.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            string symbol = fields[0];

            return symbol;

        }

        public static string GetExchange(string fullSymbol)
        {
            string[] fields = fullSymbol.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            string symbol = fields[0];
            string exchange = fields.Length >= 2 ? fields[1] : null;

            return exchange;

        }

        public static SecurityType GetSecurityType(string fullSymbol)
        {
            string[] fields = fullSymbol.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            string symbol = fields[0];
            string strSecType = fields.Length >= 3 ? fields[2] : null;
            strSecType = strSecType != _MISSING_KEY_WILDCARD ? strSecType : null;

            SecurityType secType = SecurityType.CS;//Default security type

            if (strSecType != null)
                secType = GetSecurityTypeFromStr(strSecType);

            return secType;

        }

        #endregion

    }
}

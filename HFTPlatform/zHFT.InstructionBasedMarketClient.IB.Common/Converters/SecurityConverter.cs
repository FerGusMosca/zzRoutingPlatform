using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.InstructionBasedMarketClient.IB.Common.Converters
{
    public class SecurityConverter 
    {
        #region Private Static Consts

        private static string _SECURITY_TYPE_COMMON_STOCK = "STK";
        private static string _SECURITY_TYPE_FUTURE = "FUT";
        private static string _SECURITY_TYPE_OPTIONS = "OPT";
        private static string _SECURITY_TYPE_INDEX = "IND";
        private static string _SECURITY_TYPE_COMMODITY = "CMDTY";
        private static string _SECURITY_TYPE_CASH = "CASH";

        private static string _DEF_CURRENCY = "USD";
        
        private static string _US_PRIMARY_EXCHANGE = "ISLAND";
        
        #endregion


        #region Public Static Methods
        
        public static string GetSymbol(SecurityType secType, string symbol, string currSep)
        {
            if (secType != SecurityType.CASH)
            {
                return symbol;
            }
            else
            {
                if (symbol.Contains(currSep))
                    return symbol.Split(new string[] {currSep}, StringSplitOptions.None)[0];
                else
                {
                    return symbol;
                }
            }

        }

        public static string GetPrimaryExchange(SecurityType secType)
        {

            if (secType == SecurityType.CASH || secType == SecurityType.CMDTY)
                return null;
            else
            {
                return _US_PRIMARY_EXCHANGE;
            }
        }



        public static string GetCurrency(SecurityType secType, string currency, string symbol, string currSep)
        {
            if (currency == null)
            {
                if (secType == SecurityType.CASH || secType == SecurityType.CMDTY)
                {
                    if (symbol.Contains(currSep))
                        return symbol.Split(new string[] {currSep}, StringSplitOptions.RemoveEmptyEntries)[1];
                    else
                    {
                        return _DEF_CURRENCY;
                    }
                }
                else
                {
                    return _DEF_CURRENCY;
                }
            }
            else
            {
                return currency;
            }

        }

        public static string GetSecurityType(SecurityType type)
        {
            if (type == SecurityType.CS)
                return _SECURITY_TYPE_COMMON_STOCK;
            else if (type == SecurityType.FUT)
                return _SECURITY_TYPE_FUTURE;
            else if (type == SecurityType.OPT)
                return _SECURITY_TYPE_OPTIONS;
            else if (type == SecurityType.IND)
                return _SECURITY_TYPE_INDEX;
            else if (type == SecurityType.CASH)
                return _SECURITY_TYPE_CASH;
            else if (type == SecurityType.CMDTY)
                return _SECURITY_TYPE_COMMODITY;
            else
                throw new Exception(string.Format("Could not process security type {0}", type.ToString()));

        }


        #endregion
    }
}

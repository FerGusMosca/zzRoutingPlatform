using System;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.Common.Converter
{
    public class SecurityConverterBase
    {
        #region Protected Static Consts
        
        private static string _US_PRIMARY_EXCHANGE = "ISLAND";

        private static string _CBOE_PRIMARY_EXCHANGE = "CBOE";
        
        protected static string _DEF_CURRENCY = "USD";
        
        #endregion
        
        #region Public Methods
        
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
        
        public static string GetPrimaryExchange(SecurityType secType)
        {
            if (secType == SecurityType.CASH || secType == SecurityType.CMDTY)
                return null;
            else if (secType == SecurityType.IND)
                return _CBOE_PRIMARY_EXCHANGE;
            else
            {
                return _US_PRIMARY_EXCHANGE;
            }
        }
        
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
        
        #endregion
    }
}
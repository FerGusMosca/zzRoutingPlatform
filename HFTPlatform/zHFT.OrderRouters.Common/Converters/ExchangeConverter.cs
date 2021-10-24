using System;

namespace zHFT.OrderRouters.Common.Converters
{
    public class ExchangeConverter
    {
        #region Protected Consts

        protected static string _FIELDS_SEPARATOR = ".";

        protected static string _DEFAULT_EXCHANGE = "BUE";

        protected static int _EXCHANGE_FIELD = 1;//GGAL.ROFX.FUT --> symbol full structure
        
        #endregion
        
        
        #region Public Static Methods


        public static string GetExchange(string fullSymbol)
        {
            string[] flds = fullSymbol.Split(new string[] {_FIELDS_SEPARATOR}, StringSplitOptions.RemoveEmptyEntries);


            if (flds.Length >= 2)
                return flds[_EXCHANGE_FIELD];
            else
                return _DEFAULT_EXCHANGE;
        }

        #endregion
    }
}
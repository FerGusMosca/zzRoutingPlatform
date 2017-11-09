using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.SecurityListMarketClient.Primary.Common.Converters
{
    public class ExchangeConverter
    {
        #region Private Static Conts

        private static string _BYMA = "BUE";
        private static string _BYMA_PRIMARY_PREFIX_CODE = "MERV - XMEV";


        #endregion




        #region Public Static Methods

        public static string GetMarketPrefixCode(string exchange)
        {
            if (exchange == _BYMA)
            {
                return _BYMA_PRIMARY_PREFIX_CODE;
            }
            else
                throw new Exception(string.Format("Exchange translation not implemented {0}", exchange));
        
        }

        public static string GetMarketFromFullSymbol(string fullSymbol)
        {
            if (fullSymbol.StartsWith(_BYMA_PRIMARY_PREFIX_CODE))
            {
                return _BYMA;
            }
            else
                throw new Exception(string.Format("Exchange Prefix Code translation not implemented {0}", fullSymbol));

        }

        #endregion

    }
}

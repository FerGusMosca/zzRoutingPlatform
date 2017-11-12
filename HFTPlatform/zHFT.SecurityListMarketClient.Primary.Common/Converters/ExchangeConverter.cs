using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.SecurityListMarketClient.Primary.Common.Converters
{
    public class ExchangeConverter
    {
        #region Private Static Conts

        private static string _BYMA = "BUE";
        private static string _BYMA_PRIMARY_PREFIX_CODE = "MERV - XMEV";

        private static string _BYMA_CS_CLEARINGID = "48hs";
        private static string _BYMA_OPT_CLEARINGID = "24hs";

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

        public static string GetMarketClearingID(SecurityType secType,string exchange)
        {
            if (secType == SecurityType.CS)
            {
                if (exchange == _BYMA)
                    return _BYMA_CS_CLEARINGID;
               
            }
            else if (secType == SecurityType.OPT)
            {
                if (exchange == _BYMA)
                    return _BYMA_OPT_CLEARINGID;
            }


            throw new Exception(string.Format("There is no clearing ID code set for market {0} and sec type {1}", exchange, secType.ToString()));
        
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

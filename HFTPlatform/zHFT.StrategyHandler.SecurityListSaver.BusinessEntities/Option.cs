using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.StrategyHandler.SecurityListSaver.BusinessEntities
{
    public enum PutOrCall
    {
        Call = 'C',
        Put = 'P'
    }

    public class Option:Security
    {
        #region Public Constants

        public static int _TRADING_SESSIONS = 252;
        private static int _USA_MULTIPLIER = 100;
        private static int _ARG_MULTIPLIER = 50;
        private static string _USD_CURRENCY = "USD";
        private static string _ARS_CURRENCY = "ARS";
        private static string _SMART_EXCHANGE = "SMART";

        public static char _ARG_CALL_CODE = 'C';
        public static char _ARG_PUT_CODE = 'V';

        public static string _BYMA = "BUE";
        public static string _SMART = "SMART";

        #endregion

        #region Public Attributes

        public long Id { get; set; }

        #endregion

        #region Public Methods

        public string GetSymboSfxPrefix()
        {
            if (Exchange == _BYMA)
                return Symbol.Trim().Substring(0, 3);
            else if (Exchange == _SMART)
                return Symbol.Trim().Substring(0, 3);
            else
                throw new Exception(string.Format("Exchange symbol prefix translation not implemented for exchange {0}", Exchange));
        }

        public string GetPutOrCall()
        {
            if (Exchange == _BYMA)
            {
                string callOrPutByma = Symbol.Trim().Substring(3, 1);

                if (callOrPutByma == _ARG_PUT_CODE.ToString())
                    return Convert.ToChar(zHFT.StrategyHandler.SecurityListSaver.BusinessEntities.PutOrCall.Put).ToString();
                else if (callOrPutByma == _ARG_CALL_CODE.ToString())
                    return Convert.ToChar(zHFT.StrategyHandler.SecurityListSaver.BusinessEntities.PutOrCall.Call).ToString();
                else
                    throw new Exception(string.Format("Could not recognize put or call code {0} for symbol{1}", callOrPutByma, Symbol));
              
            }
            else if (Exchange == _SMART)
                return Symbol.Trim().Substring(12, 3);
            else
                throw new Exception(string.Format("Put or call symbol translation not implemented for exchange {0}",Exchange));
        }

        public string GetStrikeCurrency()
        {
            if (Exchange == _BYMA)
                return _ARS_CURRENCY;
            else if (Exchange == _SMART)
                return _USD_CURRENCY;
            else
                throw new Exception(string.Format("Strike Currency translation not implemented for exchange {0}", Exchange));
        }

        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.SecurityListSaver.BusinessEntities
{
    public enum PutOrCall
    {
        Call = 'C',
        Put = 'P'
    }

    public class Option
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

        #endregion

        #region Public Attributes

        public long Id { get; set; }

        public string SymbolSfx { get; set; }

        public int StrikeMultiplier { get; set; }

        public PutOrCall PutOrCall { get; set; }

        public decimal StrikePrice { get; set; }

        public string StrikeCurrency { get; set; }

        public string MaturityMonthYear { get; set; }

        public DateTime MaturityDate { get; set; }

        public string Currency { get; set; }

        public string SecurityExchange { get; set; }

        public bool Expired { get; set; }

        #region Accessors

        public string PutOrCallDesc
        {
            get { return PutOrCall.ToString(); }

        }

        #endregion

        #endregion
    }
}

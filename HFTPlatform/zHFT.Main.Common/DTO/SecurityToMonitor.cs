using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.DTO
{
    public  class SecurityToMonitor
    {

        #region Public Consts


        public static string _DEFAULT_SECURITY_TYPE = "CS";

        public static string _DEFAULT_CURRENCY = "USD";

        public static string _DEFAULT_EXCHANGE = "SMART";

        #endregion

        #region Public Methods


        public string Symbol { get; set; }

        public string SecurityType { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        #endregion

        #region Public Methods

        public void LoadDefaults()
        {
            if (string.IsNullOrEmpty(SecurityType))
                SecurityType = _DEFAULT_SECURITY_TYPE;

            if (string.IsNullOrEmpty(Currency))
                Currency = _DEFAULT_CURRENCY;

            if (string.IsNullOrEmpty(Exchange))
                Exchange = _DEFAULT_EXCHANGE;
        
        }

        #endregion 
    }
}

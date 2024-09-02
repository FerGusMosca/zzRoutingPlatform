using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;

namespace tph.ChainedTurtles.Common
{
    public  class ChainedTurtleIndicator
    {
        #region Private Consts

        private static string _MULT_SYMBOL_INDICATOR = "MULT_SYMBOL_INDICATOR";

        private static string _CODE_SYMBOL_SEP = "$";

        #endregion

        #region Public Attributes

        public string Code { get; set; }

        public string Assembly { get; set; }

        public string SignalType { get; set; }


        public bool RequestPrices { get; set; }


        public SecurityToMonitor SecurityToMonitor { get; set; }

        public List<SecurityToMonitor> SecuritiesToMonitor { get; set; }

        #endregion


        #region Public Methods

        public bool IsMultipleSecurity()
        {
            if (SignalType == _MULT_SYMBOL_INDICATOR)
            {

                if (SecuritiesToMonitor != null)
                    return true;
                else
                    throw new Exception($"SignalType marked as {_MULT_SYMBOL_INDICATOR} but no SecuritiesToMonitor tag defined!");

            }
            else
            {
                if (SecurityToMonitor != null)
                    return false;
                else
                    throw new Exception($"SignalType marked as single security indicator but no SecurityToMonitor tag defined!!");
            }
        }


        public string GetIndicatorKey(SecurityToMonitor sec)
        {
            return Code + _CODE_SYMBOL_SEP + sec.Symbol;
        }

        #endregion
    }
}

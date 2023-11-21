using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Util
{
    public class TradingBacktestingManager
    {
        #region Protected Attributes

        protected static bool TradingDayActive= false;


        #endregion


        #region Public Static Methods

        public static void StartTradingDay()
        {
            TradingDayActive = true;
        
        }

        public static void EndTradingDay()
        {
            TradingDayActive =false;

        }

        public static bool IsTradingDayActive()
        {
            return TradingDayActive;
        }


        #endregion
    }
}

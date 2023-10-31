using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.Common.Util
{
    public class CandleIntervalTranslator
    {
        #region Public Static Conts


        public static string _INT_1_MIN = "1 min";

        public static string _INT_5_MIN = "5 min";

        public static string _INT_1_HOUR = "1 hour";

        public static string _INT_5_HOUR = "5 hour";

        public static string _INT_DAY = "1 day";

        #endregion

        #region Public Static Methods

        public static string GetStrInterval(CandleInterval CInterval)
        {
            if (CInterval == CandleInterval.Minute_1)
                return _INT_1_MIN;
            else if (CInterval == CandleInterval.Minute_5)
                return _INT_5_MIN;
            else if (CInterval == CandleInterval.HOUR_1)
                return _INT_1_HOUR;
            else if (CInterval == CandleInterval.HOUR_5)
                return _INT_5_HOUR;
            else if (CInterval == CandleInterval.DAY)
                return _INT_DAY;
            else
                throw new Exception($"Interval not found {CInterval}");

        }

        public static CandleInterval GetCandleInterval(string Interval)
        {
            if (Interval == _INT_1_MIN)
                return CandleInterval.Minute_1;
            else if (Interval == _INT_5_MIN)
                return CandleInterval.Minute_5;
            else if (Interval == _INT_1_HOUR)
                return CandleInterval.HOUR_1;
            else if (Interval == _INT_5_HOUR)
                return CandleInterval.HOUR_5;
            else if (Interval == _INT_DAY)
                return CandleInterval.DAY;
            else
                throw new Exception($"Interval not found {Interval}");

        }

        #endregion
    }
}

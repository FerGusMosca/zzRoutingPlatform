using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Generic;

namespace zHFT.Main.Common.Util
{
    public class MarketTimer
    {
        #region Protected Attributes

        public static List<LightSavingPeriod> LightSavingPeriods { get; set; }

        #endregion

        #region Public Methods

        private static DateTime EvalOffset(DateTime time)
        {
            if (LightSavingPeriods == null)
                return time;

            foreach (var period in LightSavingPeriods)
            {
                if (time >= period.StartDate && time <= period.EndDate)
                {
                    return time.AddHours(period.OffsetHours);
                }
            }

            return time;
        }

        public static void InitializeLightSavingPeriods(List<LightSavingPeriod> lightSavingPeriods)
        {
            LightSavingPeriods= lightSavingPeriods;
        }


        public static DateTime GetTodayDateTime(string time)
        {
            DateTime parsed = DateTime.Parse(time);
            int hour = int.Parse(parsed.ToString("hh"));
            int min = int.Parse(parsed.ToString("mm"));
            string AMPM = parsed.ToString("tt");

            hour += AMPM == "p. m." || AMPM == "PM" ? 12 : 0;

            return EvalOffset(new DateTime(DateTimeManager.Now.Year, DateTimeManager.Now.Month, DateTimeManager.Now.Day, hour, min, 0));
        }


        public static bool ValidMarketTime(string from, string to)
        {

            if (from == null || to == null)
                return true;

            DateTime dfrom = GetTodayDateTime(from);
            DateTime dto = GetTodayDateTime(to);

            return (DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday
                    && DateTime.Compare(dfrom, DateTime.Now) < 0 && DateTime.Compare(DateTime.Now, dto) < 0);


        }

        #endregion
    }
}

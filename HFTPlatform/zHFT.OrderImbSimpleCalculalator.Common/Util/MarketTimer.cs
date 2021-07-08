using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderImbSimpleCalculator.Common.Util
{
    public class MarketTimer
    {

        public static DateTime GetTodayDateTime(string time)
        {
            DateTime parsed = DateTime.Parse(time);
            int hour = int.Parse(parsed.ToString("hh"));
            int min = int.Parse(parsed.ToString("mm"));
            string AMPM = parsed.ToString("tt");

            hour += AMPM == "p. m." || AMPM == "PM" ? 12 : 0;

            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, min, 0);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Util;

namespace zHFT.StrategyHandler.MktDataDownloader.Common.Util
{
    public class TimeParser
    {

        public static DateTime GetTodayDateTime(string time)
        {

            DateTime parsed = DateTime.Parse(time);
            int hour = int.Parse(parsed.ToString("hh"));
            int min = int.Parse(parsed.ToString("mm"));
            string AMPM = parsed.ToString("tt");

            hour += AMPM == "p. m." || AMPM == "PM" ? 12 : 0;

            return new DateTime(DateTimeManager.Now.Year, DateTimeManager.Now.Month, DateTimeManager.Now.Day, hour, min, 0);
        
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            hour += AMPM == "p. m." ? 12 : 0;

            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, min, 0);
        
        }
    }
}

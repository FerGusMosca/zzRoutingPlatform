using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Util;

namespace zHFT.OrderImbSimpleCalculator.Common.Util
{
    public class MarketTimer
    {

        public static DateTime GetTodayDateTime(string time)
        {
            return zHFT.Main.Common.Util.MarketTimer.GetTodayDateTime(time);

        }


        public static bool ValidMarketTime(string from, string to)
        {
            return zHFT.Main.Common.Util.MarketTimer.ValidMarketTime(from,to);

        }
    }
}

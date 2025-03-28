using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Util
{
    public class CalendarDateManager
    {
        #region Public Static Methods

        public static int GetNaturalDaysOffset(int backwardDays)
        {
            if (backwardDays >= 0)
                throw new ArgumentException("backwardDays must be negative.");

            int daysCounted = 0;
            int naturalDays = 0;
            DateTime today = DateTime.Today;

            while (daysCounted > backwardDays) // backwardDays es negativo
            {
                naturalDays--;
                DateTime day = today.AddDays(naturalDays);
                if (IsBusinessDay(day))
                    daysCounted--;
            }

            return naturalDays;
        }

        private static bool IsBusinessDay(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday &&
                   date.DayOfWeek != DayOfWeek.Sunday;
            // Eval to add holidays if needed
        }


        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.Util
{
    public class EpochConverter
    {
        #region Public Static Methods

        public static long ConvertToMilisecondEpoch(DateTime date)
        {
            // Define the Unix epoch start date (January 1, 1970)
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Calculate the difference between the input date and the epoch
            TimeSpan timeSpan = date.ToUniversalTime() - epoch;

            // Return the difference in milliseconds
            return (long)timeSpan.TotalMilliseconds;

        }

        public static DateTime ConvertFromMillisecondsEpoch(long milliseconds)
        {
            // Define the Unix epoch start date (January 1, 1970)
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Add the milliseconds to the epoch date to get the target date
            DateTime date = epoch.AddMilliseconds(milliseconds);

            // Return the resulting DateTime in UTC
            return date;

        }

        #endregion
    }
}

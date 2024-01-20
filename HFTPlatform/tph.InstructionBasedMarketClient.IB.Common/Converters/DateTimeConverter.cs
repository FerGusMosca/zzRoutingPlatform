using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.InstructionBasedMarketClient.IB.Common.Converters
{
    public class IBDateTimeConverter
    {
        public static DateTime ConvertBarDateTime(string dateToConv)
        {
            DateTime date;
            string minFormat = "yyyyMMdd  HH:mm:ss";
            string dateFormat = "yyyyMMdd";

            if (DateTime.TryParseExact(dateToConv, minFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out date))
            {
                return date;
            }
            else if (DateTime.TryParseExact(dateToConv, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out date))
            {
                return date;

            }
            else
                throw new Exception($"Could not convert date {dateToConv} with format(s) {minFormat}/{dateFormat}");

        }

    }
}

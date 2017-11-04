using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.OptionsMarketClient.Common.Interfaces;

namespace zHFT.OptionsMarketClient.USA.Common.Converter
{
    public class OptionConverter : IOptionConverter
    {
        public DateTime ExctractMaturityDateFromSymbol(string symbol)
        {
            string date = symbol.Substring(6, 6);

            string year = date.Substring(0, 2);
            string month = date.Substring(2, 2);
            string day = date.Substring(4, 2);

            return new DateTime(2000 + Convert.ToInt32(year), Convert.ToInt32(month), Convert.ToInt32(day));
        }
    }
}

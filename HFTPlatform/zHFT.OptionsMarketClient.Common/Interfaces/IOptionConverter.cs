using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OptionsMarketClient.Common.Interfaces
{
    public interface IOptionConverter
    {
        DateTime ExctractMaturityDateFromSymbol(string symbol);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Position
{
    public class CurrencyBalance
    {
        public Dictionary<string, CurrencyAmount> detailedCurrencyBalance { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Position
{
    public class AccountDetail
    {
        public CurrencyBalance currencyBalance { get; set; }
        public AvailableToOperate availableToOperate { get; set; }
        public long settlementDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BusinessEntities;

namespace zHFT.InstructionBasedMarketClient.Binance.BusinessEntities
{
    public class AccountBinanceData
    {
        public Account Account { get; set; }

        public string APIKey { get; set; }

        public string Secret { get; set; }
    }
}

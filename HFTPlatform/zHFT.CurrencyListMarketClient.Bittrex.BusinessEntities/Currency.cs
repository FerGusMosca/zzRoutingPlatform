using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.CurrencyListMarketClient.Bittrex.BusinessEntities
{
    public class CryptoCurrency
    {

        public string Symbol { get; set; }
        public string Name { get; set; }

        public double MinConfirmation { get; set; }
        public double TxFee { get; set; }
        public bool IsActive { get; set; }
        public string CoinType { get; set; }
        public string BaseAddress { get; set; }
        public string Notice { get; set; }


    }
}

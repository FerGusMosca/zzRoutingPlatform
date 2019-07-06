using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Interface
{
    public interface IMarketCapProvider
    {
        decimal GetMarketCap(string symbol, string currency);
    }
}

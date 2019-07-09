using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntitie;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Interface
{
    public interface IMarketCapProvider
    {
        CryptoCurrency GetCryptoCurrencyData(string symbol, string currency);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccess
{
    public class DataContextFactory
    {
        public static StocksHistoricalDataEntities GetSecuritiesHistoricalDataContext(string connString)
        {
            return new StocksHistoricalDataEntities(connString);
        }
    }
}

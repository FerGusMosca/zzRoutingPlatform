using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using zHFT.StrategyHandler.SecurityListSaver.DataAccess;

namespace zHFT.StrategyHandler.SecurityListSaver.DataAccess
{
    public class DataContextFactory
    {
        public static StocksHistoricalDataEntities GetSecuritiesHistoricalDataContext(string connString)
        {
            return new StocksHistoricalDataEntities(connString);
        }
    }
}

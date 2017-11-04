using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace zHFT.StrategyHandler.OptionsContractSaver.DataAccess
{
    public class DataContextFactory
    {
        public static AutPortfolioEntities GetAutPortfolioDataContext(string connString)
        {
            return new AutPortfolioEntities(connString);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using zHFT.StrategyHandler.IBR.Primary.DataAccess;

namespace zHFT.StrategyHandler.IBR.Primary
{
    public class DataContextFactory
    {
        public static AutPortfolioEntities GetAutPortfolioDataContext(string connString)
        {
            return new AutPortfolioEntities(connString);
        }
    }
}

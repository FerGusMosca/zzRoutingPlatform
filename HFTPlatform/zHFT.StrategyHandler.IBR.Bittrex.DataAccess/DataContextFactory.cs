using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using MomentumBackTests.DataAccess;
using zHFT.StrategyHandler.IBR.Bittrex.DataAccess;

namespace MomentumBackTests.DataAccess
{
    public class DataContextFactory
    {
        public static AutPortfolioEntities GetAutPortfolioDataContext(string connString)
        {
            return new AutPortfolioEntities(connString);
        }
    }
}

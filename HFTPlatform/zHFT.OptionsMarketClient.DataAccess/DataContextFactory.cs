using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using zHFT.OptionsMarketClient.DataAccess;


namespace zHFT.OptionsMarketClient.DataAccess
{
    public class DataContextFactory
    {
        public static AutPortfolioEntities GetAutPortfolioDataContext(string connString)
        {
            return new AutPortfolioEntities(connString);
        }
    }
}

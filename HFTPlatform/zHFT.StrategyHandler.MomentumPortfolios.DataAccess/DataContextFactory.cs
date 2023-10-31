using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.MomentumPortfolios.DataAccess
{
    public class DataContextFactory
    {
        public static MomentumTradingEntities GetDataContext()
        {
            //TODO: Estructurar mejor la recuperación de la connection string
            string connString = ConfigurationManager.ConnectionStrings["MomentumTradingEntities"].ConnectionString;
            return new MomentumTradingEntities(connString);
        }

        public static MomentumTradingEntities GetDataContext(string connString)
        {
            return new MomentumTradingEntities(connString);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccess;

namespace zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer
{
    public class MappingEnabledAbstract
    {
        #region Protected Methods
        protected readonly MomentumTradingEntities ctx;
        protected AutoMapperConfiguration AutoMapperConfiguration { get; set; }
        #endregion

        #region Constructors

        public MappingEnabledAbstract(string connectionString)
        {
            ctx = DataContextFactory.GetDataContext(connectionString);
            AutoMapperConfiguration = new AutoMapperConfiguration();
            AutoMapperConfiguration.Configure();
        }


        #endregion
    }
}

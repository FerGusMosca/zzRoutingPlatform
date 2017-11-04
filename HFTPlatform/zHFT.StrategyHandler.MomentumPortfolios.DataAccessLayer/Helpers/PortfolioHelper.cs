using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccess;

namespace zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer.Helpers
{
    public static class PortfolioHelper
    {
        public static Portfolio Map(this portfolios source)
        {
            return Mapper.Map<portfolios, Portfolio>(source);
        }

        public static portfolios Map(this Portfolio source)
        {
            return Mapper.Map<Portfolio, portfolios>(source);
        }
    }
}

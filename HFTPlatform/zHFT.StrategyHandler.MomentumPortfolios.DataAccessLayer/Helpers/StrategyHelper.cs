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
    public static class StrategyHelper
    {
       
        public static Strategy Map(this estrategias source)
        {
            return Mapper.Map<estrategias, Strategy>(source);
        }

        public static estrategias Map(this Strategy source)
        {
            return Mapper.Map<Strategy, estrategias>(source);
        }
       
    }
}

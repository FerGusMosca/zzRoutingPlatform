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
    public static class PositionHelper
    {
        public static Position Map(this posiciones source)
        {
            return Mapper.Map<posiciones, Position>(source);
        }

        public static posiciones Map(this Position source)
        {
            return Mapper.Map<Position, posiciones>(source);
        }
    }
}

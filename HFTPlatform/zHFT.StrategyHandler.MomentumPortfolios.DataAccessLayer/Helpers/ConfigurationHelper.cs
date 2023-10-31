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
    public static class ConfigurationHelper
    {
        public static Configuration Map(this configuraciones source)
        {
            return Mapper.Map<configuraciones, Configuration>(source);
        }

        public static configuraciones Map(this Configuration source)
        {
            return Mapper.Map<Configuration, configuraciones>(source);
        }
    }
}

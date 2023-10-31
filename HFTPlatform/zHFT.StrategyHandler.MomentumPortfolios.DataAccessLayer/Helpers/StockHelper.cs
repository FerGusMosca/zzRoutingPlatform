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
    public static class StockHelper
    {
        public static Stock Map(this usa_stocks source)
        {
            return Mapper.Map<usa_stocks, Stock>(source);
        }

        public static usa_stocks Map(this Stock source)
        {
            return Mapper.Map<Stock, usa_stocks>(source);
        }
    }
}

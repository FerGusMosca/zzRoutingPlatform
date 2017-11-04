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
    public static class StockAlarmHelper
    {
        public static StockAlarm Map(this alarmas source)
        {
            return Mapper.Map<alarmas, StockAlarm>(source);
        }

        public static alarmas Map(this StockAlarm source)
        {
            return Mapper.Map<StockAlarm, alarmas>(source);
        }
    }
}

using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer.Profiles;

namespace zHFT.StrategyHandler.MomentumPortfolios.DataAccessLayer
{
    public class AutoMapperConfiguration
    {
        private static readonly AutoMapperConfiguration instance = new AutoMapperConfiguration();

        public AutoMapperConfiguration()
        {
            //Logger.TraceDebug("Initializing AutoMapperConfiguration");

            Mapper.Initialize(x =>
            {
                x.AddProfile<ConfigurationProfile>();
                x.AddProfile<StrategyProfile>();
                x.AddProfile<PortfolioProfile>();
                x.AddProfile<PositionProfile>();
                x.AddProfile<StockProfile>();
                x.AddProfile<StockAlarmProfile>();
            });

            //Logger.TraceDebug("AutoMapperConfiguration initialized successfully");
        }

        public void Configure()
        {
        }
    }
}

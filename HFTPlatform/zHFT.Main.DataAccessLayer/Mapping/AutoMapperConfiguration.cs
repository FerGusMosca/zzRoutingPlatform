using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.DataAccessLayer.Profiles;

namespace zHFT.StrategyHandler.DataAccessLayer
{
    public class AutoMapperConfiguration
    {
        private static readonly AutoMapperConfiguration instance = new AutoMapperConfiguration();

        public AutoMapperConfiguration()
        {
            //Logger.TraceDebug("Initializing AutoMapperConfiguration");

            Mapper.Initialize(x =>
            {
                //x.AddProfile<ExecutionReportProfile>();
                //x.AddProfile<ExecutionSummaryProfile>();
                //x.AddProfile<OrderProfile>();
                //x.AddProfile<PositionProfile>();
            });

            //Logger.TraceDebug("AutoMapperConfiguration initialized successfully");
        }

        public static AutoMapperConfiguration Instance
        {
            get
            {
                return instance;
            }
        }

        public void Configure()
        {
        }
    }
}

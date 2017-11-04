using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.DataAccess;


namespace zHFT.Main.DataAccessLayer.Helpers
{
    public static class ExecutionReportHelper
    {
        public static ExecutionReport Map(this execution_reports source)
        {
            return Mapper.Map<execution_reports, ExecutionReport>(source);
        }

        public static execution_reports Map(this ExecutionReport source)
        {
            return Mapper.Map<ExecutionReport, execution_reports>(source);
        }
    }
}

using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.DataAccess;


namespace zHFT.Main.DataAccessLayer.Helpers
{
    public static class ExecutionSummaryHelper
    {
        public static ExecutionSummary Map(this execution_summaries source)
        {
            return Mapper.Map<execution_summaries, ExecutionSummary>(source);
        }

        public static execution_summaries Map(this ExecutionSummary source)
        {
            return Mapper.Map<ExecutionSummary, execution_summaries>(source);
        }
    }
}

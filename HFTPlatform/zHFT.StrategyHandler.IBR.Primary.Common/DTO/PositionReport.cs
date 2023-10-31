using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.Primary.Common.DTO
{
    public class PositionReport
    {
        public Dictionary<string, DetailedPositions> Positions { get; set; }
    }
}

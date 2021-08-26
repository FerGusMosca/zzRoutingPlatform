using System.Collections.Generic;
using zHFT.StrategyHandler.IBR.Primary.Common.DTO;

namespace PrimaryCertification
{
    public class PositionDto
    {
        public Dictionary<string, DetailedPositions> Positions { get; set; }
    }
}
using System.Collections.Generic;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.StrategyHandler.IBR.Primary.Common.DTO;

namespace PrimaryCertification
{
    public class PositionsDto
    {
        public string account { get; set; }
        
        public Dictionary<string, Dictionary<string, DetailedPositions>> report { get; set; }
        
        public long lastCalculation { get; set; }
    }
}
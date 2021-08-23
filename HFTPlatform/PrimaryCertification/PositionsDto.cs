using zHFT.Main.BusinessEntities.Positions;

namespace PrimaryCertification
{
    public class PositionsDto
    {
        public string account { get; set; }
        
        public PositionDto report { get; set; }
        
        public long lastCalculation { get; set; }
    }
}
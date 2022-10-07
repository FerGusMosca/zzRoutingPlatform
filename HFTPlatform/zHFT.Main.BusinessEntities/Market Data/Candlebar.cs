using System;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.Main.BusinessEntities.Market_Data
{
    public class Candlebar
    {
        #region Public Attributes
        
        public Security Security { get; set; }
        
        public string Key { get; set; }
        
        public DateTime Date { get; set; }
        
        
        public double? High { get; set; }
        
        public double? Open { get; set; }
        
        public double? Close { get; set; }
        
        public double? Low { get; set; }
        
        public double? Trade { get; set; }
        
        public int Volume { get; set; }
        
        #endregion
    }
}
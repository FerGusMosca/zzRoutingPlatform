using System.Collections.Generic;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;

namespace DayCurrenciesTrading.Common.Configuration
{
    public class Configuration: BaseConfiguration
    {
        #region Public Attributes
        
        [XmlArray]
        [XmlArrayItem(ElementName = "Pair")]
        public List<string> PairToMonitor { get; set; }
        
        public int MovAvgShort { get; set; }
        
        public int MovAvgLong { get; set; }
        
        public string IncomingModule { get; set; }
        
        public string IncomingConfigPath { get; set; }
        
        public string OutgoingModule { get; set; }
        
        public string OutgoingConfigPath { get; set; }
        
        public double PositionSizeInCash { get; set; }
        
        public int MaxPositionsInPortfolio { get; set; }
        
        
        #endregion
        
        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool res = true;
            if (PairToMonitor.Count==0)
            {
                result.Add("PairToMonitor");
                res = false;
            }

            if (MovAvgShort<=0)
            {
                result.Add("MovAvgShort");
                res = false;
            }
            
            if (MovAvgLong<=0)
            {
                result.Add("MovAvgLong");
                res = false;
            }

            if (string.IsNullOrEmpty(IncomingModule))
            {
                result.Add("IncomingModule");
                res = false;
            }
            
            if (string.IsNullOrEmpty(IncomingConfigPath))
            {
                result.Add("IncomingConfigPath");
                res = false;
            }
            
            if (PositionSizeInCash<=0)
            {
                result.Add("PositionSizeInCash");
                res = false;
            }
            
//            if (string.IsNullOrEmpty(OutgoingModule))
//            {
//                result.Add("OutgoingModule");
//                res = false;
//            }

            return res;
        }
        
        #endregion
    }
}
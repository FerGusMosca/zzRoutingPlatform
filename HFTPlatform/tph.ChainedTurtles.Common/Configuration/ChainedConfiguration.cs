using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using tph.DayTurtles.Common.Configuration;
using tph.TrendlineTurtles.Common.Configuration;
using zHFT.Main.Common.DTO;

namespace tph.ChainedTurtles.Common.Configuration
{
    public class ChainedConfiguration : TrendlineConfiguration
    {
        //Chained logic --> assembly
        //Chain setting --> open signal type
        
        public bool BulkMarketDataRequest { get; set; }

        public bool? SkipTrendlinesDeletion { get; set; }

        #region Public Attributes


        public List<SecurityToMonitor> SecuritiesToMonitor { get; set; }

        public List<ChainedTurtleIndicator> ChainedTurtleIndicators { get; set; }

        #endregion

    }
}

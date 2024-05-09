using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.Common.Configuration;
using tph.BOBDayTurtles.Common.Configuration;

namespace tph.ManualTrendlinesAdvisorTurtles.Common.Configuration
{
    public class AdvisorConfiguration : tph.BOBDayTurtles.Common.Configuration.Configuration
    {
        #region Public Attributes

        public   string YahooPricesDownloadURL { get; set; }

        public string YahooTokenURL { get; set; }   

        public string YahooPostfix { get; set; }


        #endregion
    }
}

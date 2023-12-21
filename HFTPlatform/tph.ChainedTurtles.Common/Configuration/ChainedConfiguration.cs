using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using tph.DayTurtles.Common.Configuration;

namespace tph.ChainedTurtles.Common.Configuration
{
    public class ChainedConfiguration : DayTurtlesConfiguration
    {
        //Chained logic --> assembly
        //Chain setting --> open signal type
        //

        #region Public Attributes


        public List<TurtleConfig> TurtleConfigs{ get; set; }

        #endregion

    }
}

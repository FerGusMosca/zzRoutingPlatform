using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class ImbalanceTurtleIndicatorConfigDTO
    {
        #region Public Attributes

        public string marketStartTime { get; set; }

        public string marketEndTime { get; set; }

        public string maxOpeningTime { get; set; }

        public string closingTime { get; set;}

        public int blockSizeInMinutes { get; set; }

        public int activeBlocksSetting { get; set;}

        #endregion
    }
}

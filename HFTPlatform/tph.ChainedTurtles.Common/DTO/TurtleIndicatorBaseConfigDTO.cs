using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class TurtleIndicatorBaseConfigDTO
    {
        #region Public Attributes

        public string marketStartTime { get; set; }

        public string marketEndTime { get; set; }

        public string maxOpeningTime { get; set; }

        public string closingTime { get; set; }


        #endregion
    }
}

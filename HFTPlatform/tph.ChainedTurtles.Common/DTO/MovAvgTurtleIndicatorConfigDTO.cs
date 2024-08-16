using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class MovAvgTurtleIndicatorConfigDTO : TurtleIndicatorBaseConfigDTO
    {
        #region Public Attributes

        public int avgPeriod { get; set; }

        #endregion
    }
}

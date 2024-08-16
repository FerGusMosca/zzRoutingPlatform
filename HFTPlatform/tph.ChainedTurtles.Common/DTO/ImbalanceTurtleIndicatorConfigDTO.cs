using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class ImbalanceTurtleIndicatorConfigDTO: TurtleIndicatorBaseConfigDTO
    {
        #region Public Attributes

        public int blockSizeInMinutes { get; set; }

        public int activeBlocksSetting { get; set;}

        public decimal positionOpeningImbalanceThreshold { get; set; }

        public decimal positionClosingImbalanceThreshold { get; set; }

        #endregion
    }
}

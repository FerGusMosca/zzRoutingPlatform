using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;

namespace tph.ChainedTurtles.Common
{
    public  class ChainedTurtleIndicator
    {
        #region Public Attributes

        public string Code { get; set; }

        public string Assembly { get; set; }

        public string SignalType { get; set; }


        public bool RequestPrices { get; set; }


        public SecurityToMonitor SecurityToMonitor { get; set; }

        #endregion
    }
}

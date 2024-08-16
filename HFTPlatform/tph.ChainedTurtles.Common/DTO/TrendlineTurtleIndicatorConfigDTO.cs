using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{


    public class TrendlineTurtleIndicatorConfigDTO : TurtleIndicatorBaseConfigDTO
    {
        #region Public Attributes

        public int innerTrendlinesSpan { get; set; }

        public int outterTrendlinesSpan { get; set; }

        public double perforationThresholds { get;set; }

        public string candleReferencePrice { get; set; }
        
        public bool? recalculateTrendlines { get; set; }

        public int skipCandlesToBreakTrndln { get; set; }

        #endregion
    }
}

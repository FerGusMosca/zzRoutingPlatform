using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.ChainedTurtles.Common.DTO
{
    public class TrendlineTurtleIndicatorConfigDTO
    {
        #region Public Attributes

        public string marketStartTime { get; set; }

        public string marketEndTime { get; set; }

        public string maxOpeningTime { get; set; }

        public string closingTime { get; set; }


        public int innerTrendlinesSpan { get; set; }

        public int outterTrendlinesSpan { get; set; }

        public double perforationThresholds { get;set; }

        public string candleReferencePrice { get; set; }
        
        public bool? recalculateTrendlines { get; set; }

        public int historicalPricesPeriod { get; set; }

        public int skipCandlesToBreakTrndln { get; set; }

        #endregion
    }
}

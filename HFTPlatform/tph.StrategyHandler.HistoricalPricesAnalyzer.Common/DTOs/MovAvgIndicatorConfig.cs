using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.StrategyHandler.HistoricalPricesAnalyzer.Common.DTOs
{
    public class MovAvgIndicatorConfig
    {
        #region Pubic Attributes

        public string Key { get; set; }

        public int Window { get; set; }

        #endregion

    }
}

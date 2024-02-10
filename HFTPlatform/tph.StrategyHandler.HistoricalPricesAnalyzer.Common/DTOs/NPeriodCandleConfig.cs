using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.StrategyHandler.HistoricalPricesAnalyzer.Common.DTOs
{
    public class NPeriodCandleConfig
    {
        #region Pubic Attributes

        public string Key { get; set; }

        public int Period { get; set; }

        public string RecalculationDay { get; set; }


        #endregion
    }
}

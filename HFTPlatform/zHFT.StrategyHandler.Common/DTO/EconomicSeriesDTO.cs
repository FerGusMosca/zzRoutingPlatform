using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;

namespace zHFT.StrategyHandler.Common.DTO
{
    public class EconomicSeriesDTO
    {
        #region Public Attributs

        public string SeriesID { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public CandleInterval Interval { get; set; }

        public List<EconomicSeriesValue> Values { get; set; }

        public bool Success { get; set; }

        public string Error { get; set; }

        #endregion
    }
}

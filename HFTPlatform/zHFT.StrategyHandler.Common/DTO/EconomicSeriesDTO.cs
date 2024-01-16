using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;

namespace zHFT.StrategyHandler.Common.DTO
{
    public class EconomicSeriesDTO
    {
        #region Public Attributs

        public string SeriesID { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public List<EconomicSeriesValue> Values { get; set; }

        #endregion
    }
}

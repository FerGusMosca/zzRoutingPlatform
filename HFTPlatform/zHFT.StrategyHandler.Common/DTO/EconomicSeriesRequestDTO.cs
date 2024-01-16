using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.Common.DTO
{
    public class EconomicSeriesRequestDTO
    {
        #region Public Attributes

        public  string SeriesID { get; set; }


        public DateTime From { get; set; }
        
        public DateTime To { get; set; }

        #endregion
    }
}

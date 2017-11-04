using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities
{
    public class Portfolio
    {
        #region Public Attributes
        public int Id { get; set; }
        public IList<Position> Positions { get; set; }
        public DateTime ProcessingStartingDate { get; set; }
        public DateTime ProcessingEndingDate { get; set; }

        public List<StockAlarm> Alarms { get; set; }
        public Strategy Strategy { get; set; }
        public int StocksEvaluated { get; set; }
        #endregion
    }
}

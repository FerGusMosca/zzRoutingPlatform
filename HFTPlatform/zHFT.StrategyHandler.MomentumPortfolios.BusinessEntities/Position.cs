using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities
{
    public class Position
    {
        #region Public Attributes
        public int Id { get; set; }
        public Stock Stock { get; set; }
        public string MarketCap { get; set; }
        public double Weight { get; set; }
        public double WinnerRatio { get; set; }
        public Portfolio Portfolio { get; set; }

        public bool Processed { get; set; }
        #endregion
    }
}

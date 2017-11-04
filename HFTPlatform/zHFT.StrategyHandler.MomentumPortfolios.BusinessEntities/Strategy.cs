using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.MomentumPortfolios.BusinessEntities
{
    public class Strategy
    {
        #region Public Attributes
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public Configuration Configuration { get; set; }
        public List<Portfolio> Portfolios { get; set; }

        #endregion
    }
}

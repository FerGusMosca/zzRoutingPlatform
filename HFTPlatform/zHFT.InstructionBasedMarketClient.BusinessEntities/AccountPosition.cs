using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.InstructionBasedMarketClient.BusinessEntities
{
    public class AccountPosition
    {
        #region Public Attributes

        public long Id { get; set; }

        public Account Account { get; set; }

        public Security Security { get; set; }

        public decimal? Weight { get; set; }

        public int? Shares { get; set; }

        public decimal? MarketPrice { get; set; }

        public decimal? Ammount { get; set; }

        public PositionStatus PositionStatus { get; set; }

        public bool Active { get; set; }

        #endregion
    }
}

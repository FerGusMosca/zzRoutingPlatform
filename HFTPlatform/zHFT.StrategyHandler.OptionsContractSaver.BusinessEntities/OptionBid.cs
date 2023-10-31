using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.StrategyHandler.OptionsContractSaver.BusinessEntities
{
   

    public class OptionBid
    {
        #region Public Attributes

        public Option Option { get; set; }

        public DateTime Timestamp { get; set; }

        public int Size { get; set; }

        public decimal Price { get; set; }

        public Side Side { get; set; }

        #region Underlying Attributes

        public decimal? UnderlyingPrice { get; set; }

        #endregion

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CLS.CryptoCompare.Common.DTO
{
    public class MarketDataDisplayDTO
    {
        #region Public Attributes

        public string FROMSYMBOL { get; set; }

        public string TOSYMBOL { get; set; }

        public string MARKET { get; set; }

        public string PRICE { get; set; }

        public string MKTCAP { get; set; }

        #endregion

    }
}

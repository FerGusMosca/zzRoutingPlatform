using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CLS.CryptoCompare.Common.DTO
{
    public class MarketDataRawDTO
    {
        #region Public Attributes

        public string FROMSYMBOL { get; set; }

        public string TOSYMBOL { get; set; }

        public string MARKET { get; set; }

        public decimal PRICE { get; set; }

        public decimal MKTCAP { get; set; }

        #endregion
    }
}

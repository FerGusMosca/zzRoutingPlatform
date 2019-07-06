using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntities
{
    public class CryptoInPortfolio : SecurityInPortfolio
    {
        #region Public Attributes

        public string QuoteCurrency { get; set; }

        public string Exchange { get; set; }

        #endregion
    }
}

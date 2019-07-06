using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntities
{
    public class SecurityInPortfolio : Security
    {
        public string Folder { get; set; }

        public SecurityParameter SecurityParameter { get; set; }
    }
}

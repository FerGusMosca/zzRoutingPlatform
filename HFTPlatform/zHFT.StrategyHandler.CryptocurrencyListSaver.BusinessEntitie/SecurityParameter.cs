using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntities
{
    public class SecurityParameter
    {
        #region Public Attributes

        public Security Stock { get; set; }

        public decimal PerforationThreshold { get; set; }

        public int? MMovShort { get; set; }

        public int? MMovLong { get; set; }

        public int? StchK { get; set; }

        public int? StchSlow { get; set; }

        public int? StchD { get; set; }

        public int? StchUpperBand { get; set; }

        public int? StchLowerBand { get; set; }

        public string Symbol
        {
            get
            {
                return Stock != null ? Stock.Symbol : null;
            }
            set
            {
                if (Stock == null)
                    Stock = new Security();

                Stock.Symbol = value;
            }
        }

        #endregion
    }
}

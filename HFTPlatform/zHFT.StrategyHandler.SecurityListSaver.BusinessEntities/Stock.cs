using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.SecurityListSaver.BusinessEntities
{
    public class Stock
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public string Name { get; set; }

        public Market Market { get; set; }

        public string Category { get; set; }

        public string Country { get; set; }

        #endregion

        #region Public Methods

        public void LoadFinalSymbol()
        {
            if (Country == null)
                throw new Exception("Country not specified!");

            if (Country != Market._DEFAULT_COUNTRY)
            {
                if (!Symbol.Contains(string.Format(".{0}", Country)))
                {
                    Symbol += "." + Market.Code;
                }
            }
        }

        #endregion
    }
}

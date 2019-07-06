using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntities
{
    public class Security
    {
        #region Public Attributes
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Market { get; set; }
        public string Country { get; set; }
        public string Category { get; set; }
        public double MarketCap { get; set; }
        public SecurityParameter SecurityParameter { get; set; }
        #endregion


        #region Public Methods

        public string GetMarketCap()
        {
            if (MarketCap > 1000000000)//billions
                return Math.Round(MarketCap / Convert.ToDouble(1000000000), 2) + "B";
            else if (MarketCap > 1000000)//millions
                return Math.Round(MarketCap / Convert.ToDouble(1000000), 2) + "M";
            else if (MarketCap > 1000)//millions
                return Math.Round(MarketCap / Convert.ToDouble(1000), 2) + "K";
            else
                return Math.Round(MarketCap, 2).ToString();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.SecurityList;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.Position
{
    public class PortfolioPosition
    {
        public Security instrument { get; set; }
        public string symbol { get; set; }
        public double buySize { get; set; }
        public double buyPrice { get; set; }
        public double sellSize { get; set; }
        public double sellPrice { get; set; }
        public double totalDailyDiff { get; set; }
        public double totalDiff { get; set; }
        public string tradingSymbol { get; set; }
        public double originalBuyPrice { get; set; }
        public double originalSellPrice { get; set; }
        public double originalBuySize { get; set; }
        public double originalSellSize { get; set; }
    }
}

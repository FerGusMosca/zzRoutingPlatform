using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.MktDataDownloader.BE
{
    public class BondMarketData
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public string Timestamp { get; set; }

        public string SettlDate { get; set; }

        public DateTime Datetime { get; set; }

        public decimal LastTrade { get; set; }

        public decimal BestBidPrice { get; set; }

        public decimal BestBidSize { get; set; }

        public decimal BestAskPrice { get; set; }

        public decimal BestAskSize { get; set; }

        #endregion

        #region Public Methods

        public decimal GetARSMEPAskDepth(List<BondMarketData> timestampBonds)
        {
            string dSymbol = Symbol + "D";
            BondMarketData bondD = timestampBonds.Where(x => x.Symbol == dSymbol && x.SettlDate == SettlDate).FirstOrDefault();

            if (Symbol.EndsWith("D") || Symbol.EndsWith("C") || bondD == null)
                return 0;
            else
            {
                if (BestAskPrice > 0 && bondD.BestBidPrice > 0)
                {
                    decimal arsDepth = BestAskPrice * (BestAskSize / 100);
                    decimal usdDepth = bondD.BestBidPrice * (bondD.BestBidSize / 100);
                    decimal implTC = BestAskPrice / bondD.BestBidPrice;

                    if (arsDepth > (usdDepth * implTC))
                        return usdDepth * implTC;
                    else
                        return arsDepth;
                }
                else
                    return 0;
            }
        }

        public decimal GetARSMEPBidDepth(List<BondMarketData> timestampBonds)
        {
            string arsSymbol = Symbol.Substring(0, Symbol.Length - 1);

            BondMarketData bondARS = timestampBonds.Where(x => x.Symbol == arsSymbol && x.SettlDate == SettlDate).FirstOrDefault();

            if (!Symbol.EndsWith("D") || bondARS == null)
                return 0;
            else
            {
                if (BestAskPrice != 0 && bondARS.BestBidPrice != 0)
                {
                    decimal usdDepth = BestAskPrice * (BestAskSize / 100);
                    decimal arsDepth = bondARS.BestBidPrice * (bondARS.BestBidSize / 100);
                    decimal implTC = bondARS.BestBidPrice / BestAskPrice;

                    if (arsDepth > (usdDepth * implTC))
                        return usdDepth * implTC;
                    else
                        return arsDepth;
                }
                else
                    return 0;
            }
        }

        #endregion
    }
}

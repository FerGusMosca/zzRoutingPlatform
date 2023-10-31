using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO
{
    public class OrderBookEntry
    {
        #region Protected Static Consts

        private static string _BUY = "Buy";

        private static string _SELL = "Sell";

        #endregion

        #region Public Attributes

        public string symbol { get; set; }

        public long id { get; set; }

        public string side { get; set; }

        public decimal size { get; set; }

        public decimal price { get; set; }

        #endregion

        #region Public Methods

        public bool IsBuy()
        {
            return side == _BUY;
        }

        public bool IsSell()
        {
            return side == _SELL;
        }

        #endregion
    }
    //public class OrderBookEntry
    //{
    //    #region Private Conts

    //    private static string _SELL = "Sell";

    //    private static string _BUY = "Buy";

    //    #endregion

    //    #region Public Attributes

    //    public string symbol { get; set; }

    //    public long id { get; set; }

    //    public string side { get; set; }

    //    public decimal size { get; set; }

    //    public decimal price { get; set; }

    //    #endregion

    //    public static decimal GetBestBidSize(List<OrderBookEntry> entries)
    //    {

    //        decimal bestBidPrice = 0;
    //        decimal bestBidSize = 0;

    //        foreach (OrderBookEntry entry in entries.Where(x => x.side == _BUY).OrderByDescending(x=>x.price))
    //        {
    //            if (entry.price >= bestBidPrice)
    //            {
    //                bestBidPrice = entry.price;
    //                bestBidSize += entry.size;
    //            }
            
    //        }

    //        return bestBidSize;
    //    }

    //    public static decimal GetBestAskSize(List<OrderBookEntry> entries)
    //    {

    //        decimal bestAskPrice = decimal.MaxValue;
    //        decimal bestAskSize = 0;

    //        foreach (OrderBookEntry entry in entries.Where(x => x.side == _SELL).OrderBy(x => x.price))
    //        {
    //            if (entry.price <= bestAskPrice)
    //            {
    //                bestAskPrice = entry.price;
    //                bestAskSize += entry.size;
    //            }

    //        }

    //        return bestAskSize;
    //    }
    //}
}

using System.Linq;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.Main.BusinessEntities.Market_Data
{
    public class OrderBook
    {
        #region Public Attributes
        
        public Security Security { get; set; }
        
        public OrderBookEntry[] Bids { get; set; }
        
        public OrderBookEntry[] Asks { get; set; }
        
        #endregion
        
        #region Public Methods

        public string GetTopOfBook()
        {
            string resp = "";

            if (Bids != null && Bids.Length > 0)
            {
                OrderBookEntry bestBid = Bids.OrderByDescending(x => x.Price).FirstOrDefault();

                resp += string.Format(" Best Bid Px={0}  Best Bid  Size={1}", bestBid.Price, bestBid.Size);

            }
            else
                resp += " no best  bid";

            resp+=" - ";

            if (Asks != null && Asks.Length > 0)
            {
                OrderBookEntry bestAsk = Asks.OrderBy(x => x.Price).FirstOrDefault();

                resp += string.Format(" Best Ask Px={0}  Best Ask  Size={1}", bestAsk.Price, bestAsk.Size);

            }
            else
                resp += " no best  ask";

            return resp;
        }

        #endregion
    }
}
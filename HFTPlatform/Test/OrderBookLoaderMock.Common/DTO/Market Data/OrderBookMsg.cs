using System;
using System.Linq;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace OrderBookLoaderMock.Common.DTO
{
    public class OrderBookMsg: WebSocketMessage
    {
        #region Public Static Consts

        public static int _MAX_ORDER_BOOK_ORDERS_PER_SIDE = 5;
        
        #endregion
        
        #region Public Attributes
        
        public Security Security { get; set; }
        
        public OrderBookEntry[] Bids { get; set; }
        
        public OrderBookEntry[] Asks { get; set; }
        
        #endregion
        
        #region Public Methods

        public static int RoundQty(decimal size, bool noZeros=true)
        {
            int qty = Convert.ToInt32(Math.Floor(size));

            if (qty == 0 && noZeros)
                qty = 1;

            return qty;
        }

        public string GetStrEntry(int pos)
        {
            OrderBookEntry bid = GetEntry(Side.Buy, pos);

            OrderBookEntry ask = GetEntry(Side.Sell, pos);

            string line = "";

            line += string.Format("{0} - {1}", bid != null ? OrderBookMsg.RoundQty(bid.Size).ToString() : "0", bid != null ? bid.Price.ToString() : "x");
            line += "   ";
            line+=string.Format("{0} - {1}", ask != null ? ask.Price.ToString() : "x", ask != null ? OrderBookMsg.RoundQty(ask.Size).ToString() : "0");

            return line;

        }

        public OrderBookEntry GetEntry(Side side, int pos)
        {
            if (side == Side.Buy)
            {
                if (Bids == null)
                    return null;
                if (pos < Bids.Length)
                    return Bids[pos];
                else
                    return null;
            }

            else if (side == Side.Sell)
            {
                if (Asks == null)
                    return null;
                if (pos < Asks.Length)
                    return Asks[pos];
                else
                    return null;
            }
            else
                throw new Exception(string.Format("Invalid Side {0} recovering order book entry", side));
            
            


        }

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
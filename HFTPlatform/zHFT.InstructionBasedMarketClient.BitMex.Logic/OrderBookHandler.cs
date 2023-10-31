using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;

namespace zHFT.InstructionBasedMarketClient.BitMex.Logic
{
    public class OrderBookHandler
    {
        #region Public Attributes

        public  Dictionary<string, OrderBookDictionary> OrderBooks { get; set; }

        private static object tLock = new object();

        #endregion

        #region Constructors

        public OrderBookHandler()
        {
            OrderBooks = new Dictionary<string, OrderBookDictionary>();
        }

        #endregion

        #region Public Methods

        public void RunPriceLevelCreation(string pair, OrderBookEntry[] bids, OrderBookEntry[] asks)
        {
            OrderBookDictionary newDic = new OrderBookDictionary();

            foreach (OrderBookEntry bid in bids.OrderByDescending(x => x.price).Take(5))
            {
                newDic.Entries.Add(bid.id, bid);
            }

            foreach (OrderBookEntry ask in asks.OrderBy(x => x.price).Take(5))
            {
                newDic.Entries.Add(ask.id, ask);
            }

            Console.WriteLine();

            if (!OrderBooks.ContainsKey(pair))
            {
                OrderBooks.Add(pair, newDic);
            }
            else
            {
                OrderBooks.Remove(pair);
                OrderBooks.Add(pair, newDic);
            }
        }

        public void RunPriceLevelUpdate(string pair, OrderBookEntry[] bids, OrderBookEntry[] asks)
        {
            if (OrderBooks.ContainsKey(pair))
            {
                OrderBookDictionary dic = OrderBooks[pair];

                foreach (OrderBookEntry bid in bids)
                {
                    if (dic.Entries.ContainsKey(bid.id))
                    {
                        OrderBookEntry entry = dic.Entries[bid.id];
                        entry.size = bid.size;
                    }
                    //else
                    //   throw new Exception(string.Format("Could not find price level for pair {0} with id {1}", pair, bid.id));
                }

                foreach (OrderBookEntry ask in asks)
                {
                    if (dic.Entries.ContainsKey(ask.id))
                    {
                        OrderBookEntry entry = dic.Entries[ask.id];
                        Console.WriteLine(string.Format("ASK UPDATE SIZE={0} PRICE={1}", ask.size.ToString("##.##"), entry.price.ToString("##.########")));
                        entry.size = ask.size;
                    }
                    //else
                    //    throw new Exception(string.Format("Could not find price level for pair {0} with id {1}", pair, ask.id));
                }

            }
            else
                throw new Exception(string.Format("Unknown Error: Order Book not found in memory for pair {0}", pair));

        }

        public void RunPriceLevelDelete(string pair, OrderBookEntry[] bids, OrderBookEntry[] asks)
        {
            if (OrderBooks.ContainsKey(pair))
            {
                OrderBookDictionary dic = OrderBooks[pair];

                foreach (OrderBookEntry bid in bids)
                {
                    if (dic.Entries.ContainsKey(bid.id))
                    {
                        OrderBookEntry entry = dic.Entries[bid.id];
                        dic.Entries.Remove(bid.id);
                    }
                    //else
                    //    throw new Exception(string.Format("Could not find price level for pair {0} with id {1}", pair, bid.id));
                }

                foreach (OrderBookEntry ask in asks)
                {
                    if (dic.Entries.ContainsKey(ask.id))
                    {
                        OrderBookEntry entry = dic.Entries[ask.id];
                        dic.Entries.Remove(ask.id);
                    }
                    //else
                    //    throw new Exception(string.Format("Could not find price level for pair {0} with id {1}", pair, ask.id));
                }

            }
            else
                throw new Exception(string.Format("Unknown Error: Order Book not found in memory for pair {0}", pair));

        }

        public void RunPriceLevelNew(string pair, OrderBookEntry[] bids, OrderBookEntry[] asks)
        {
            if (OrderBooks.ContainsKey(pair))
            {
                OrderBookDictionary dic = OrderBooks[pair];

                foreach (OrderBookEntry bid in bids)
                {
                    if (!dic.Entries.ContainsKey(bid.id))
                    {
                        dic.Entries.Add(bid.id, bid);
                    }
                    else
                       throw new Exception(string.Format("Invalid new message for existing price level for pair {0} with id {1}", pair, bid.id));
                }

                foreach (OrderBookEntry ask in asks)
                {
                    if (!dic.Entries.ContainsKey(ask.id))
                    {
                        Console.WriteLine(string.Format("ASK NEW SIZE={0} PRICE={1}", ask.size.ToString("##.##"), ask.price.ToString("##.########")));
                        dic.Entries.Add(ask.id, ask);
                    }
                    else
                        throw new Exception(string.Format("Invalid new message for existing price level for pair {0} with id {1}", pair, ask.id));
                }

            }
            else
                throw new Exception(string.Format("Unknown Error: Order Book not found in memory for pair {0}", pair));

        }

        public void DoUpdateOrderBooks(string action, OrderBookEntry[] bids, OrderBookEntry[] asks)
        { 
            string pair = "";
            if (bids.Length > 0)
                pair = bids[0].symbol;
            else if (asks.Length > 0)
                pair = asks[0].symbol;
            else
                return;

            if (action == "partial")//we have the full order book
                RunPriceLevelCreation(pair, bids, asks);
            else
            {
                lock (tLock)//Here we are going to change the dictionary so we have to be careful to make it thread safe
                {
                    //We have a partial update
                    if (action == "update")
                        RunPriceLevelUpdate(pair, bids, asks);
                    else if (action == "delete")
                        RunPriceLevelDelete(pair, bids, asks);
                    else if (action == "insert")
                        RunPriceLevelNew(pair, bids, asks);
                    else
                        throw new Exception(string.Format("Invalid action for pair {0}:{1}", pair, action));
                }
            }
        
        }

        #endregion
    }
}

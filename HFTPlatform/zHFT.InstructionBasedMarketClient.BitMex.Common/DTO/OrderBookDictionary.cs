using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO
{
    public class OrderBookDictionary
    {
        #region Constructors

        public OrderBookDictionary()
        {
            Entries = new Dictionary<long, OrderBookEntry>();
        }

        #endregion

        #region Public Attributes

        public Dictionary<long, OrderBookEntry> Entries { get; set; }

        #endregion
    }
}

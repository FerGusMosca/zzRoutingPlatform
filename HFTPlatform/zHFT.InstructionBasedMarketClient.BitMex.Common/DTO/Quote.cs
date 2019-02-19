using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO
{
    public class Quote
    {
        #region Public Attributes

        public DateTime timestamp { get; set; }

        public string symbol { get; set; }

        public decimal bidSize { get; set; }

        public decimal bidPrice { get; set; }

        public decimal askPrice { get; set; }

        public decimal askSize { get; set; }

        #endregion
    }
}

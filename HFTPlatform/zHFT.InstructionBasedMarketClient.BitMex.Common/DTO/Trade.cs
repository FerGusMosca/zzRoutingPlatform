using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO
{
    public class Trade
    {
        #region Public Attributes

        public DateTime timestamp { get; set; }

        public string symbol { get; set; }

        public string side { get; set; }

        public decimal size { get; set; }

        public decimal price { get; set; }

        public string tickDirection { get; set; }

        public string trdMatchID { get; set; }

        public decimal grossValue { get; set; }

        public decimal homeNotional { get; set; }

        public decimal foreignNotional { get; set; }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTDPairTradingDemoLibrary.DTO
{
    public class PairTradingRequest
    {
        #region Public Attributes

        public string LongSymbol { get; set; }

        public string ShortSymbol { get; set; }

        public decimal ConvertionRatio { get; set; }

        public decimal PlusCash { get; set; }

        public decimal SpreadLong { get; set; }

        public decimal? SpreadUnwind { get; set; }

        public int QtyLong { get; set; }

        public int? MaxUnhedgedAmmount { get; set; }

        public string InitiateFirst { get; set; }

        public string Account { get; set; }

        public string Broker { get; set; }

        #endregion
    }
}

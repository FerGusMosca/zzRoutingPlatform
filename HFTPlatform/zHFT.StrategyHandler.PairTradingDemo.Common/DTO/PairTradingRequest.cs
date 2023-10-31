using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;

namespace zHFT.StrategyHandler.PairTradingDemo.Common.DTO
{
    public class PairTradingRequest
    {
        #region Public Attributes

        public int Id { get; set; }

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


        #region Status Attributes

        public bool Opened { get; set; }

        public bool MarketDataRequestSent { get; set; }

        public double? CurrentSpread { get; set; }

        public double? LastLong { get; set; }

        public double? LastShort { get; set; }

        public MarketData LastMDLong { get; set; }

        public MarketData LastMDShort { get; set; }

        public string LastStatus { get; set; }

        public ExecutionReport LastERLong { get; set; }

        public ExecutionReport LastERShort { get; set; }


        #endregion

        #region Public Methods

        public double GetPosQty(bool longPos)
        {
            if (longPos)
                return QtyLong;
            else
                return QtyLong * Convert.ToDouble(ConvertionRatio);
        }

        public bool EvalReadyToBeOpened()
        {
            if (LastLong.HasValue && LastShort.HasValue)
            {
                CurrentSpread = ((LastShort.Value * Convert.ToDouble(ConvertionRatio)) + Convert.ToDouble(PlusCash)) - LastLong.Value;

                if (CurrentSpread.Value < Convert.ToDouble(SpreadLong))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public string GetCurrentSpread()
        {
            return CurrentSpread.HasValue ? CurrentSpread.Value.ToString("##.##") : "<no market data yet>";
        }


        #endregion
    }
}

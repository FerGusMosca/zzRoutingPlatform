using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OptionsMarketClient.BusinessEntities
{
    public enum PutOrCall
    {
        Call = 'C',
        Put = 'P'
    }

    public class Option
    {
        #region Public Attributes

        public long Id { get; set; }

        public string Symbol { get; set; }

        public string SymbolSfx { get; set; }

        public int StrikeMultiplier { get; set; }

        public PutOrCall PutOrCall { get; set; }

        public double StrikePrice { get; set; }

        public string StrikeCurrency { get; set; }

        public string MaturityMonthYear { get; set; }

        public DateTime MaturityDate { get; set; }

        public string Currency { get; set; }

        public string SecurityExchange { get; set; }

        public bool Expired { get; set; }

        #region MarketData Attributes

        public int? ReqId { get; set; }

        public double? TradeVolume { get; set; }

        public double? ClosingPrice { get; set; }

        #endregion

        #endregion

        #region Public Static Methods

        public static int ProcessMultiplier(string multiplier)
        {
            try
            {
                return Convert.ToInt32(multiplier);
            }
            catch(Exception)
            {
                throw new Exception(string.Format("Could not convert to an integer value fore the Strike Multiplier the value {0}", multiplier));
            }
        }

        public static PutOrCall ProcessPutOrCall(string right)
        {
            try
            {
                return (PutOrCall) Convert.ToChar(right);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("Could not process put or call value: {0}", right));
            }
        }


        #endregion
    }
}

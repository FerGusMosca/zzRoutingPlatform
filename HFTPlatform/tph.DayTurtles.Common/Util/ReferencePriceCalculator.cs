using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;

namespace tph.DayTurtles.Common.Util
{
    public class ReferencePriceCalculator
    {

        #region Public Static Consts

        public static string _CANDLE_REF_PRICE_TRADE = "TRADE";
        public static string _CANDLE_REF_PRICE_CLOSE = "CLOSE";

        #endregion

        #region Public Static Methods


        public static double? GetReferencePrice(MarketData md,string CandleReferencePrice)
        {
            if (string.IsNullOrEmpty(CandleReferencePrice))
            {
                return md.ClosingPrice;
            }
            else if (CandleReferencePrice == _CANDLE_REF_PRICE_CLOSE)
            {
                return md.ClosingPrice;
            }
            else if (CandleReferencePrice == _CANDLE_REF_PRICE_TRADE)
            {
                return md.Trade;
            }
            else
            {
                throw new Exception(string.Format("Candle Reference  Price not recognized:{0}", CandleReferencePrice));
            }
        }

        #endregion
    }
}

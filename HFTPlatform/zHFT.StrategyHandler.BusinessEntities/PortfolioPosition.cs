using System;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public class PortfolioPosition
    {
        
        #region Public Static Consts

        public static string _CANLDE_REF_PRICE_TRADE = "TRADE";
        public static string _CANLDE_REF_PRICE_CLOSE = "CLOSE";
        
        #endregion
        
        #region Public Attributes
        
        public Security Security { get; set; }
        
        public bool Closing { get; set; }
        
        
        public int? DecimalRounding { get; set; }

        public string CandleReferencePrice { get; set; }

        #endregion

        #region public Methods


        public virtual bool AppendCandle(MarketData md) {

            return true;
        }

        public virtual string SignalTriggered()
        {
            return "";

        }

        public bool IsClosing()
        {
            return Closing;
        }

        public double? GetReferencePrice(MarketData md)
        {
            if (string.IsNullOrEmpty(CandleReferencePrice))
            {
                return md.ClosingPrice;
            }
            else if (CandleReferencePrice == _CANLDE_REF_PRICE_CLOSE)
            {
                return md.ClosingPrice ;
            }
            else if (CandleReferencePrice == _CANLDE_REF_PRICE_TRADE)
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
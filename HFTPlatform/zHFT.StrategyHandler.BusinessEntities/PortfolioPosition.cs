using System;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public class PortfolioPosition
    {
        
      
        
        #region Public Attributes
        
        public Security Security { get; set; }
        
        public bool Closing { get; set; }
        
        
        public int? DecimalRounding { get; set; }

        public string CandleReferencePrice { get; set; }

        #endregion

        #region public Methods

        public virtual bool AppendCandleHistorical(MarketData md) { return true; }


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

        #endregion


    }
}
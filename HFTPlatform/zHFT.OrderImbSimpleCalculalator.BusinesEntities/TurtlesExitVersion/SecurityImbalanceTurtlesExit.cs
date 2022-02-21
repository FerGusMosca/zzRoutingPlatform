using System.Collections.Generic;
using zHFT.Main.BusinessEntities.Market_Data;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class SecurityImbalanceTurtlesExit:SecurityImbalance
    {
        #region Constructors

        public SecurityImbalanceTurtlesExit()
        {
            Candles= new Dictionary<string, MarketData>();
        }

        #endregion
        
        #region Protected Attributes
        
        public Dictionary<string, MarketData> Candles { get; set; }
        
        public int CloseWindow { get; set; }
        
        public int OppTrendClosingWindow { get; set; }
        
        public string CandleReferencePrice { get; set; }
        
        #endregion
        
        #region Protected Methods
        
        public virtual bool AppendCandle(MarketData md)
        {
            bool newCandle = false;
            if (md.MDEntryDate.HasValue)
            {
                string key = md.MDEntryDate.Value.ToString("yyyyMMddHHmm");
                
                if (!Candles.ContainsKey(key))
                {
                    newCandle = true;
                    Candles.Add(key, md);
                }
                else
                    Candles[key] = md;

            }
            
            Security.MarketData = md;
            return newCandle;
        }
        
        #endregion
        
        #region Public Overriden Methods
        
        public override void AppendMarketData(MarketData md)
        {
            base.AppendMarketData(md);
            AppendCandle(md);
        }
        
        #endregion
    }
}
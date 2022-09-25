using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using zHFT.Main.BusinessEntities.Market_Data;

namespace DayCurrenciesTrading.BusinessEntities.TechnicalIndicators
{
    public class ExponentialMovingAverage
    {
        #region Constructors

        public ExponentialMovingAverage(int pLength)
        {
            Length = pLength;
            
            Prices = new Dictionary<string, MarketData>();
            Initialized = false;

            WeightMultiplier = 2 / (pLength + 1);
        }

        #endregion

        #region Public Attributes

        public int Length { get; set; }

        public Dictionary<string, MarketData> Prices { get; set; }

        public MarketData LastUpdatedPrice { get; set; }
        
        #region Internal Control
        
        protected bool Initialized { get; set; }
        
        public  double Average { get; set; }
        
        protected  double PreviousAverage { get; set; }
        
        protected double Slope { get; set; }
        
        protected double WeightMultiplier { get; set; }
        
        #endregion

        #endregion

        #region Protected Method

        protected void UpdateMovingAverage(MarketData md)
        {
            if(!md.ClosingPrice.HasValue)
                throw new Exception(String.Format("ERROR critical situation when closing price does not have a value for security {0} @UpdateMovingAverage",md.Security.Symbol));
            
            if (!Initialized)
            {
                Average = md.ClosingPrice.Value;

                PreviousAverage = Average;

                Initialized = true;

                Slope = 0;

                return;

            }

            Average = ((md.ClosingPrice.Value - PreviousAverage) * WeightMultiplier) + PreviousAverage;
            
            Slope = Average - PreviousAverage;

            PreviousAverage = Average;
        }

        #endregion
        
        #region Public Attributes

        public void UpdatePrice(MarketData md)
        {
            if (!md.MDLocalEntryDate.HasValue)
                return;
            
            lock (Prices)
            {
                string key = md.MDLocalEntryDate.Value.ToString("yyyyMMddhhmm");
                LastUpdatedPrice = md;

                if (!Prices.ContainsKey(key))

                {
                    //We process the previous minute!
                    if(Prices.Values.Count>0)
                        UpdateMovingAverage(Prices.Values.ToArray()[Prices.Values.Count - 1]);
                    Prices.Add(key,md);
                }
                else
                {
                    
                    Prices[key] = md;
                }
            }
        }

        
        public double? GetLastPrice()
        {
            if (LastUpdatedPrice != null)
                return LastUpdatedPrice.Trade;
            else
                return null;
        }

        #endregion
    }
}
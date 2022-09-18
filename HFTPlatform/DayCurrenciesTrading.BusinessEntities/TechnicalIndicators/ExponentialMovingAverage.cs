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

        public ExponentialMovingAverage(int pMovAvgLong, int pMovAvgShort)
        {
            MovAvgLong = pMovAvgLong;
            MovAvgShort = pMovAvgShort;
            Prices =new Dictionary<string,MarketData>();
        }

        #endregion
        
        #region Public Attributes
        
        public int MovAvgLong { get; set; }
        
        public int MovAvgShort { get; set; }
        
        public Dictionary<string,MarketData> Prices { get; set; } 
        
        public MarketData LastUpdatedPrice { get; set; }
        
        #endregion
        
        #region Public Attributes

        public void UpdatePrice(MarketData md)
        {
            if (!md.MDEntryDate.HasValue)
                return;
            
            lock (Prices)
            {
                string key = md.MDEntryDate.Value.ToString("yyyyMMddhhmm");
                LastUpdatedPrice = md;

                if (!Prices.ContainsKey(key))
                            
                {
                    Prices.Add(key,md);
                }
                else
                {
                    Prices[key] = md;
                }
            }
        }

        public double? CalculateShortAverage()
        {
            //TODO Calculate MMOVAVG
            return 0;

        }
        
        public double? CalculateLongAverage()
        {
            //TODO Calculate MMOVAVG
            return 0;

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
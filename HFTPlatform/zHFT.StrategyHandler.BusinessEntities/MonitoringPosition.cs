using System;
using System.Collections.Generic;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public class MonitoringPosition
    {
        
        #region Public Attributes
        
        public Security Security { get; set; }

        public Dictionary<string, MarketData> Candles { get; set; }

        public bool Closing { get; set; }
        
        
        public int? DecimalRounding { get; set; }

        public string CandleReferencePrice { get; set; }

        #endregion

        #region public Methods

        public virtual List<MonitoringPosition> GetInnerIndicators()
        {
            return new List<MonitoringPosition>();//empty
        }

        public virtual bool IsTrendlineMonPosition()
        {
            return false;
        
        }

        public virtual MarketData GetLastTriggerPrice()
        {
            return null;
        }

        public virtual bool AppendCandleHistorical(MarketData md) { return true; }


        public virtual bool AppendCandle(MarketData md) {

            return true;
        }

        public virtual string SignalTriggered()
        {
            return "";

        }

        //Activates the signal indicator or other statistical calculations depending the monitoring position role in the trading strategy
        public virtual bool EvalSignalTriggered()
        {
            return false;
        }



        public bool IsClosing()
        {
            return Closing;
        }

        public virtual MarketData GetLastFinishedCandle(int cowntdown)
        {
            if (Candles.Count > (cowntdown + 1))
                return Candles.Values.OrderByDescending(x => x.GetOrderingDate()).ToArray()[cowntdown];
            else
                return null;
        }

        #endregion


    }
}
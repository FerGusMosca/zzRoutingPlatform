using System;
using System.Collections.Generic;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using static zHFT.Main.Common.Util.Constants;

namespace zHFT.StrategyHandler.BusinessEntities
{
    public class MonitoringPosition
    {
        
        #region Public Attributes

        protected ILogger Logger { get; set; }
        
        public Security Security { get; set; }

        public Dictionary<string, MarketData> Candles { get; set; }

        public bool Closing { get; set; }
        
        
        public int? DecimalRounding { get; set; }

        public string CandleReferencePrice { get; set; }

        public MonitoringType MonitoringType { get; set; }

        #endregion

        #region public Methods

        public void DoLog(string msg, MessageType type)
        { 
            if(Logger!=null)
                Logger.DoLog(msg, type);
        
        }

        public virtual List<MonitoringPosition> GetInnerIndicators()
        {
            return new List<MonitoringPosition>();//empty
        }

        public virtual bool IsTrendlineMonPosition()
        {

            return MonitoringType==MonitoringType.ONLY_TRENDLINE || MonitoringType==MonitoringType.TRENDLINE_PLUS_ROUTING ;
        
        }

        public virtual MarketData GetLastTriggerPrice()
        {
            return null;
        }

        public virtual bool AppendCandleHistorical(MarketData md) { return true; }

        public virtual string RelevantInnerInfo() { return "None"; }


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


        public virtual List<Security> GetSecurities()
        {

            List<Security> securities = new List<Security>();

            securities.Add(Security);

            return securities;
        }

        #endregion


    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;

namespace tph.BOBDayTurtles.BusinessEntities
{
    public class MonBOBTurtlePosition:MonTurtlePosition
    {
        #region Constructors
        
        public MonBOBTurtlePosition(int openWindow, int closeWindow, double stopLossForOpenPositionPct) : base(openWindow, closeWindow, stopLossForOpenPositionPct)
        {
        }
        
        #endregion
        
        #region Protected Attributes
        
        public List<Trendline> Resistances { get; set; }
        
        public List<Trendline> Supports { get; set; }
        
        #endregion
        
        #region Protected Methods
        
        protected bool EvalResistanceBroken()
        {
            List<MarketData> histPrices = new List<MarketData>(Candles.Values);
            histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

            MarketData lastClosedCandle = GetLastFinishedCandle();
            bool found = false;
            List<Trendline> activeResistances = Resistances.Where(x => x.TrendlineType == TrendlineType.Resistance
                                                                         && x.BrokenDate == null).ToList();
            foreach (Trendline trendline in activeResistances)
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                if (lastClosedCandle.ClosingPrice > trendlinePrice)
                {
                    trendline.BrokenDate = lastClosedCandle.MDEntryDate;
                    trendline.JustBroken = true;
                    found = true;
                }
            }

            return found;
        }
        
        protected bool EvalSupportBroken()
        {

            List<MarketData> histPrices = new List<MarketData>(Candles.Values);
            histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

            MarketData lastClosedCandle = GetLastFinishedCandle();
            bool found = false;
            List<Trendline> activeSupports = Supports.Where(x => x.TrendlineType == TrendlineType.Support
                                                                 && x.BrokenDate == null).ToList();
            foreach (Trendline trendline in activeSupports)
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                if (lastClosedCandle.ClosingPrice < trendlinePrice)
                {
                    trendline.BrokenDate = lastClosedCandle.MDEntryDate;
                    trendline.JustBroken = true;
                    found = true;
                }
            }

            return found;
        }
        
        #endregion
        
        #region Public Methods

        public virtual bool HasHistoricalCandles()
        {
            return Candles.Keys.Count > 0;
        }

        public virtual bool AppendCandle(MarketData md)
        {
            return base.AppendCandle(md);
        }

        public MarketData GetLastCandle()
        {
            return Candles.Values.OrderByDescending(x => x.MDEntryDate.Value).FirstOrDefault();
        }
        
        public MarketData GetLastFinishedCandle()
        {
            return Candles.Values.OrderByDescending(x => x.MDEntryDate.Value).ToArray()[1];
        }

        public void PopulateTrendlines(List<Trendline> resistances,List<Trendline> supports)
        {
            Resistances = resistances;
            Supports = supports;

        }

        public override bool LongSignalTriggered()
        {
            return EvalResistanceBroken();
            
        }
        
        public override bool ShortSignalTriggered()
        {
            return EvalSupportBroken();
        }
        
        public override string SignalTriggered()
        {
            //It logs information abou the signal that has been triggered

            Trendline resistance = Resistances.Where(x => x.JustBroken).FirstOrDefault();
            Trendline support = Supports.Where(x => x.JustBroken).FirstOrDefault();

            if (resistance != null)
            {
                return string.Format(" --> Broken Resistance: Start={0} End={1}  ",resistance.StartDate, resistance.EndDate);
            }
            
            else if (support != null)
            {
                return string.Format(" --> Broken Support: Start={0} End={1}  ",support.StartDate, support.EndDate);
            }
            else
            {
                return "";
            }

        }

        #endregion
    }
}
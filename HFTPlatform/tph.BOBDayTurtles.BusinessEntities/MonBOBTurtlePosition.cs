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
            
            foreach (Trendline trendline in Resistances.Where(x=>x.BrokenDate==null))
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                if (lastClosedCandle.ClosingPrice > trendlinePrice)
                {
                    trendline.BrokenDate = lastClosedCandle.MDEntryDate;
                    return true;
                }
            }

            return false;
        }
        
        protected bool EvalSupportBroken()
        {

            List<MarketData> histPrices = new List<MarketData>(Candles.Values);
            histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

            MarketData lastClosedCandle = GetLastFinishedCandle();
            
            foreach (Trendline trendline in Resistances.Where(x=>x.BrokenDate==null))
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                if (lastClosedCandle.ClosingPrice < trendlinePrice)
                {
                    trendline.BrokenDate = lastClosedCandle.MDEntryDate;
                    return true;
                }
            }

            return false;
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

        #endregion
    }
}
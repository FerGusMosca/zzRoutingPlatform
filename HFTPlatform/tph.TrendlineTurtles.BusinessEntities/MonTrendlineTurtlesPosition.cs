using System;
using System.Collections.Generic;
using System.Linq;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;

namespace tph.TrendlineTurtles.BusinessEntities
{
    public class MonTrendlineTurtlesPosition:MonTurtlePosition
    {
        #region Protected Attributes

        public List<Trendline> Resistances { get; set; }

        public List<Trendline> Supports { get; set; }

        protected int OuterSignalSpan { get; set; }
        
        public  Trendline LastOpenTrendline { get; set; }

        #endregion
        
        #region Constructors
        public MonTrendlineTurtlesPosition(int openWindow, int closeWindow, bool pExitOnMMov, double stopLossForOpenPositionPct, string candleRefPrice) : base(openWindow, closeWindow, pExitOnMMov, stopLossForOpenPositionPct, candleRefPrice)
        {
            Supports=new List<Trendline>();
            Resistances=new List<Trendline>();
        }
        
        #endregion
        
        #region Protected Attributes
        
        public void PopulateTrendlines(List<Trendline> resistances,List<Trendline> supports)
        {
            resistances.ForEach(x=>Resistances.Add(x));
            supports.ForEach(x=>Supports.Add(x));
        }

      
        public void AppendSupport(Trendline support)
        {
            if (!Supports.Any(x =>
                DateTime.Compare(x.StartDate, support.StartDate) == 0 &&
                DateTime.Compare(x.EndDate, support.EndDate) == 0))
            {
                Supports.Add(support);
            }
        }
        
        public void AppendResistance(Trendline resistance)
        {
            if (!Resistances.Any(x =>
                DateTime.Compare(x.StartDate, resistance.StartDate) == 0 &&
                DateTime.Compare(x.EndDate, resistance.EndDate) == 0))
            {
                Resistances.Add(resistance);
            }
        }
        
        #endregion
        
        #region Protected Methods
          
        protected bool EnoughSpan(Trendline trendline, MarketData md)
        {
              TimeSpan elapsed = md.MDEntryDate.Value - trendline.EndDate;
              return elapsed.TotalMinutes >= OuterSignalSpan;
         }

        protected bool EvalResistanceBroken()
        {
            List<MarketData> histPrices = GetHistoricalPrices();

            MarketData lastClosedCandle = GetLastFinishedCandle();
            bool found = false;
            List<Trendline> activeResistances = Resistances.Where(x => x.TrendlineType == TrendlineType.Resistance
                                                                       && !x.IsBroken(lastClosedCandle.MDEntryDate)
                                                                       && x.ValidDistanceToEndDate(lastClosedCandle.MDEntryDate.Value,OuterSignalSpan,CandleInterval.Minute_1)
                                                                       && x.IsSoftSlope(histPrices)).ToList();
            foreach (Trendline trendline in activeResistances)
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                if (lastClosedCandle.BiggerGreendCandle(trendlinePrice))
                {
                    trendline.BrokenDate = lastClosedCandle.MDEntryDate;
                    trendline.BrokenTrendlinePrice = trendlinePrice;
                    trendline.BrokenMarketPrice = lastClosedCandle;
                    trendline.JustBroken = true;
                    trendline.Persisted = false;
                    if (EnoughSpan(trendline, lastClosedCandle))
                    {
                        found = true;
                        LastOpenTrendline = trendline;
                    }
                }

            }

            return found;
        }

        protected bool EvalSupportBroken()
        {

            List<MarketData> histPrices = GetHistoricalPrices();

            MarketData lastClosedCandle = GetLastFinishedCandle();
            bool found = false;
            List<Trendline> activeSupports = Supports.Where(x => x.TrendlineType == TrendlineType.Support
                                                                 && !x.IsBroken(lastClosedCandle.MDEntryDate)
                                                                 && x.ValidDistanceToEndDate(lastClosedCandle.MDEntryDate.Value,OuterSignalSpan,CandleInterval.Minute_1)
                                                                 && x.IsSoftSlope(histPrices)).ToList();
            foreach (Trendline trendline in activeSupports)
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                if (lastClosedCandle.LowerRedCandle(trendlinePrice))
                {
                    trendline.BrokenDate = lastClosedCandle.MDEntryDate;
                    trendline.BrokenTrendlinePrice = trendlinePrice;
                    trendline.BrokenMarketPrice = lastClosedCandle;
                    trendline.JustBroken = true;
                    trendline.Persisted = false;
                    if (EnoughSpan(trendline, lastClosedCandle))
                    {
                        LastOpenTrendline = trendline;
                        found = true;
                    }
                }
            }

            return found;
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;

namespace tph.BOBDayTurtles.BusinessEntities
{
    public class MonBOBTurtlePosition : MonTurtlePosition
    {
        #region Constructors

        public MonBOBTurtlePosition(int openWindow, int closeWindow, double stopLossForOpenPositionPct,
            int outerSignalSpan) : base(openWindow, closeWindow, stopLossForOpenPositionPct)
        {
            Resistances = new List<Trendline>();
            Supports = new List<Trendline>();
            OuterSignalSpan = outerSignalSpan;
        }

        #endregion

        #region Protected Attributes

        public List<Trendline> Resistances { get; set; }

        public List<Trendline> Supports { get; set; }

        protected int OuterSignalSpan { get; set; }
        
        public  Trendline LastOpenTrendline { get; set; }

        #endregion

        #region Protected Methods

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
        
        public DateTime GetLastCandleDate()
        {
            MarketData lastCandle = GetLastCandle();

            if (lastCandle != null && lastCandle.MDEntryDate.HasValue)
                return lastCandle.MDEntryDate.Value;
            else
                return DateTime.Now;
        }

        public MarketData GetLastFinishedCandle()
        {
            return Candles.Values.OrderByDescending(x => x.MDEntryDate.Value).ToArray()[1];
        }

        public List<MarketData> GetHistoricalPrices()
        {
            List<MarketData> histPrices = new List<MarketData>(Candles.Values);
            return histPrices.OrderBy(x => x.MDEntryDate).ToList();
        }

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
                MarketData lastCandle = GetLastCandle();
                List<MarketData> histPrices = GetHistoricalPrices();
                double trendlinePrice = resistance.CalculateTrendPrice(lastCandle.MDEntryDate.Value, histPrices);
                return string.Format(" --> Broken Resistance: Start={0} End={1} Now={2} LastCandlePrice={3} LastCandleDate={4} TrendlinePrice={5}  ",
                                    resistance.StartDate, resistance.EndDate,DateTime.Now,lastCandle.ClosingPrice,lastCandle.MDEntryDate.Value,
                                    trendlinePrice);
            }
            
            else if (support != null)
            {
                MarketData lastCandle = GetLastCandle();
                List<MarketData> histPrices = GetHistoricalPrices();
                double trendlinePrice = support.CalculateTrendPrice(lastCandle.MDEntryDate.Value, histPrices);
                return string.Format(" --> Broken Support: Start={0} End={1} Now={2} LastCandlePrice={3} LastCandleDate={4} TrendlinePrice={5}  ",
                    support.StartDate, support.EndDate,DateTime.Now,lastCandle.ClosingPrice,lastCandle.MDEntryDate.Value,
                    trendlinePrice);
            }
            else
            {
                return "";
            }

        }

        protected bool EnoughSpan(Trendline trendline, MarketData md)
        {
            TimeSpan elapsed = md.MDEntryDate.Value - trendline.EndDate;
            return elapsed.TotalMinutes >= OuterSignalSpan;
        }

        #endregion
    }
}
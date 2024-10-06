using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;

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
        public MonTrendlineTurtlesPosition(TurtlesCustomConfig pTurtlesCustomWindow, double stopLossForOpenPositionPct, string candleRefPrice, string marketStartTime=null, string marketEndTime=null) : base(pTurtlesCustomWindow, stopLossForOpenPositionPct, candleRefPrice)
        {
            Supports=new List<Trendline>();
            Resistances=new List<Trendline>();

            MonitoringType = zHFT.Main.Common.Enums.MonitoringType.TRENDLINE_PLUS_ROUTING;
        }
        
        #endregion
        
        #region Public Methods
        
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
            DoLog($"DBG8.1-EVAL Enough Span: MDEntryDate={md.MDEntryDate.Value} trendlineEndDate={trendline.EndDate}", Constants.MessageType.Information);
            DoLog($"DBG8.2-EVAL Enough Span: ElapsedMin={elapsed.TotalMinutes} OuterSignalSpan={OuterSignalSpan}", Constants.MessageType.Information);
            return elapsed.TotalMinutes >= OuterSignalSpan;
         }

        protected List<Trendline> GetActiveSupports()
        {

            List<MarketData> histPrices = GetHistoricalPrices();

            MarketData lastClosedCandle = GetLastFinishedCandle(1);
            bool found = false;
            List<Trendline> activeSupports = Supports.Where(x => x.TrendlineType == TrendlineType.Support
                                                                 && !x.IsBroken(lastClosedCandle.MDEntryDate)
                                                                 && x.ValidDistanceToEndDate(histPrices, lastClosedCandle.MDEntryDate.Value, OuterSignalSpan, CandleInterval.Minute_1)
                                                                 && x.IsSoftSlope(histPrices)).ToList();

            return activeSupports;
        }


        protected List<Trendline> GetActiveResistances()
        {

            List<MarketData> histPrices = GetHistoricalPrices();

            MarketData lastClosedCandle = GetLastFinishedCandle(1);
            List<Trendline> activeResistances = Resistances.Where(x => x.TrendlineType == TrendlineType.Resistance
                                                                           && !x.IsBroken(lastClosedCandle.MDEntryDate)
                                                                           && x.ValidDistanceToEndDate(histPrices, lastClosedCandle.MDEntryDate.Value, OuterSignalSpan, CandleInterval.Minute_1)
                                                                           && x.IsSoftSlope(histPrices)).ToList();

            return activeResistances;
        }

        public override  string RelevantInnerInfo() 
        {

            string resp = System.Environment.NewLine+"";
            resp += $"MovAvg={CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}" + System.Environment.NewLine;
            resp+= ListActiveTrendlines();
            return resp;
        
        }

        protected string ListActiveTrendlines()
        {
            List<MarketData> histPrices = GetHistoricalPrices();

            MarketData lastClosedCandle = GetLastFinishedCandle(1);

            string resp = "";

            List<Trendline> actResistances = GetActiveResistances();
            foreach (Trendline resistance in actResistances)
            {
                double trendlinePrice = resistance.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                resp += $"==> Act. Resistance:Start Date={resistance.StartDate} EndDate={resistance.EndDate} Res Price={trendlinePrice}" + System.Environment.NewLine;
            }

            List<Trendline> actSupports = GetActiveSupports();
            foreach (Trendline support in actSupports)
            {
                double trendlinePrice = support.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                resp += $"==> Act. Support:Start Date={support.StartDate} EndDate={support.EndDate} Res Price={trendlinePrice}" + System.Environment.NewLine;
            }

            return resp;

        }

        protected bool EvalResistanceBroken()
        {
            List<MarketData> histPrices = GetHistoricalPrices();

            MarketData lastClosedCandle = GetLastFinishedCandle(1);

            bool found = false;
            List<Trendline> activeResistances = GetActiveResistances();
            foreach (Trendline trendline in activeResistances)
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                DoLog($"DBG6r-Eval Resistance--> lastCandle={lastClosedCandle.Trade} trendline={trendlinePrice}  trendline_start={trendline.StartDate} trendline_end={trendline.EndDate}", Constants.MessageType.Information);
                if (lastClosedCandle.BiggerGreendCandle(trendlinePrice))
                {
                    DoLog($"DBG7r-BROKEN!", Constants.MessageType.Information);
                    trendline.DoBreak(lastClosedCandle, histPrices);

                    if (EnoughSpan(trendline, lastClosedCandle))
                    {
                        DoLog($"DBG7r-ENOUGH-SPAN!", Constants.MessageType.Information);
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

            MarketData lastClosedCandle = GetLastFinishedCandle(1);
            bool found = false;
            List<Trendline> activeSupports = GetActiveSupports();

            foreach (Trendline trendline in activeSupports)
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);
                DoLog($"DBG6s-Eval Support--> lastCandle={lastClosedCandle.Trade} trendline={trendlinePrice} trendline_start={trendline.StartDate} trendline_end={trendline.EndDate}", Constants.MessageType.Information);
                if (lastClosedCandle.LowerRedCandle(trendlinePrice))
                {
                    DoLog($"DBG7s-BROKEN!",Constants.MessageType.Information);
                    trendline.DoBreak(lastClosedCandle, histPrices);
                    if (EnoughSpan(trendline, lastClosedCandle))
                    {
                        DoLog($"DBG7s-ENOUGH-SPAN!", Constants.MessageType.Information);
                        LastOpenTrendline = trendline;
                        found = true;
                    }
                }
            }

            return found;
        }

        public override string SignalTriggered()
        {
            try
            {
                //It logs information abou the signal that has been triggered

                Trendline resistance = Resistances.Where(x => x.JustBroken).FirstOrDefault();
                Trendline support = Supports.Where(x => x.JustBroken).FirstOrDefault();

                if (resistance != null)
                {
                    MarketData lastCandle = GetLastFinishedCandle();
                    if (lastCandle != null)
                    {
                        List<MarketData> histPrices = GetHistoricalPrices();
                        double trendlinePrice = resistance.CalculateTrendPrice(lastCandle.MDEntryDate.Value, histPrices);
                        return $"{Security.Symbol} --> Broken Resistance: Start={resistance.StartDate} End={resistance.EndDate} Now={DateTimeManager.Now} " +
                            $"LastCandlePrice={lastCandle.Trade} LastCandleDate={lastCandle.MDEntryDate.Value} TrendlinePrice={trendlinePrice} ";
                    }
                    else
                        return $"{Security.Symbol} --> NO SIGNAL- NO CANDLES";
                }

                else if (support != null)
                {
                    MarketData lastCandle = GetLastFinishedCandle();
                    if (lastCandle != null)
                    {
                        List<MarketData> histPrices = GetHistoricalPrices();
                        double trendlinePrice = support.CalculateTrendPrice(lastCandle.MDEntryDate.Value, histPrices);
                        return $"{Security.Symbol} --> Broken Support: Start={resistance.StartDate} End={resistance.EndDate} Now={DateTimeManager.Now} " +
                               $"LastCandlePrice={lastCandle.Trade} LastCandleDate={lastCandle.MDEntryDate.Value} TrendlinePrice={trendlinePrice} ";
                    }
                    else
                        return $"{Security.Symbol} --> NO SIGNAL- NO CANDLES";
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                return $"ERROR calculating signal triggered:{ex.Message}";
            
            }

        }

        #endregion
    }
}
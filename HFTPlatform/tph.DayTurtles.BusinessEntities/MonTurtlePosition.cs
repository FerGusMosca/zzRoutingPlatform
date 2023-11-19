using System;
using System.Collections.Generic;
using System.Linq;
using tph.DayTurtles.Common.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.DayTurtles.BusinessEntities
{
    public class MonTurtlePosition : MonitoringPosition
    {
        #region Protected Static Consts


        protected static string _CANDLE_DATE_FORMAT = "yyyyMMddHHmm";

        #endregion

        #region Constructors

        public MonTurtlePosition()
        {
            Candles = new Dictionary<string, MarketData>();
        
        }


        public MonTurtlePosition(int openWindow, int closeWindow, bool pExitOnMMov, double stopLossForOpenPositionPct, string candleRefPrice)
        {
            Candles = new Dictionary<string, MarketData>();
            OpenWindow = openWindow;
            CloseWindow = closeWindow;
            StopLossForOpenPositionPct = stopLossForOpenPositionPct;
            CandleReferencePrice = candleRefPrice;
            ExitOnMMov = pExitOnMMov;
        }

        #endregion

        #region Protected Attributes

        public int OpenWindow { get; set; }

        public int CloseWindow { get; set; }

        public bool ExitOnMMov { get; set; }

        public double StopLossForOpenPositionPct { get; set; }

        public Dictionary<string, MarketData> Candles { get; set; }

        protected string LastSignalTriggered { get; set; }

        #endregion

        #region Private Methods

        protected bool CanCreateCandle(MarketData md)
        {
            string key = "";

            if (md.MDEntryDate.HasValue)
                return true;
            else if (md.MDLocalEntryDate.HasValue)
                return true;
            else
                return false;
        }

        protected string GetCandleKey(MarketData md)
        {
            string key = "";

            if (md.MDEntryDate.HasValue)
                key = md.MDEntryDate.Value.ToString(_CANDLE_DATE_FORMAT);
            else if (md.MDLocalEntryDate.HasValue)
                key = md.MDLocalEntryDate.Value.ToString(_CANDLE_DATE_FORMAT);
            else
            {

                throw new Exception($"Market data entry date not available in new market data");
            }

            return key; ;

        }

        public MarketData HighestOnWindow(int window)
        {
            List<string> keys = Candles.Keys.Skip(Candles.Count - window - 1).ToList();
            MarketData highest = null;
            foreach (string key in keys)
            {
                if (highest == null)
                    highest = Candles[key];
                else
                {
                    if (highest.Trade < Candles[key].Trade)
                        highest = Candles[key];
                }
            }

            return highest;
        }

        public MarketData LowestOnWindow(int window)
        {
            List<string> keys = Candles.Keys.Skip(Candles.Count - window - 1).ToList();
            MarketData lowest = null;
            foreach (string key in keys)
            {
                if (lowest == null)
                    lowest = Candles[key];
                else
                {
                    if (lowest.Trade > Candles[key].Trade)
                        lowest = Candles[key];
                }
            }

            return lowest;
        }

        public bool IsBigger(MarketData md, MarketData lastCandle)
        {
            return ReferencePriceCalculator.GetReferencePrice(md,CandleReferencePrice) > ReferencePriceCalculator.GetReferencePrice(lastCandle,CandleReferencePrice);

        }

        public bool Islower(MarketData md, MarketData lastCandle)
        {
            return ReferencePriceCalculator.GetReferencePrice(md,CandleReferencePrice) < ReferencePriceCalculator.GetReferencePrice(lastCandle,CandleReferencePrice);

        }

        public double CalculateSimpleMovAvg()
        {
            return CalculateSimpleMovAvg(CloseWindow);
        }

        public double CalculateSimpleMovAvg(int window)
        {
            List<MarketData> windowcandles = Candles.Values.Where(x => x.GetOrderingDate() != null)
                                              .OrderByDescending(x => x.GetOrderingDate().Value)
                                              .Take(window).ToList();

            //calculate the avg
            double sum = 0;
            int count = 0;
            foreach (MarketData md in windowcandles)
            {
                if (ReferencePriceCalculator.GetReferencePrice(md,CandleReferencePrice) != null)
                {
                    sum += ReferencePriceCalculator.GetReferencePrice(md,CandleReferencePrice).Value;
                    count++;
                }
            }

            double avg = sum / Convert.ToDouble(count);
            return avg;
        }

        public bool IsHigherThanMMov(int window,bool higherOrEqual)
        {
            MarketData lastCandle = LastValidCandle();

            double avg = CalculateSimpleMovAvg(window);

            if (ReferencePriceCalculator.GetReferencePrice(lastCandle,CandleReferencePrice) != null)
            {
                if(higherOrEqual)
                    return ReferencePriceCalculator.GetReferencePrice(lastCandle,CandleReferencePrice).Value >= avg;
                else
                    return ReferencePriceCalculator.GetReferencePrice(lastCandle,CandleReferencePrice).Value > avg;

            }
            else
                return false; 

        }

        public MarketData LastValidCandle()
        {
            MarketData lastCandle = Candles.Values
                                                   .Where(x => x.GetOrderingDate() != null 
                                                            && ReferencePriceCalculator.GetReferencePrice(x,CandleReferencePrice)!=null)
                                                   .OrderByDescending(x => x.GetOrderingDate()).FirstOrDefault();


            return lastCandle;
        }
        
        public bool IsHighest(int window)
        {
            if (Candles.Count < window)
                return false;

            MarketData lastCandle = LastValidCandle();

            List<MarketData> candles = Candles.Values
                                                      .Where(x=>  x.GetOrderingDate()!=null)
                                                      .OrderByDescending(x => x.GetOrderingDate())
                                                      .Skip(1).Take(window).ToList();
            
            //We ignore the last candle which is the current candle
            bool foundBigger = false;
            foreach (MarketData md in candles)
            {
                if ( 
                    IsBigger(md,lastCandle)
                    && md.GetOrderingDate().HasValue && lastCandle.GetOrderingDate().HasValue
                    && DateTime.Compare(md.GetOrderingDate().Value, lastCandle.GetOrderingDate().Value) < 0)
                {
                    foundBigger = true;
                    break;
                }
            }

            return !foundBigger;
            
        }
        
        public bool IsLowest(int window)
        {
            if (Candles.Count < window)
                return false;

            MarketData lastCandle = LastValidCandle();

            List<MarketData> candles = Candles.Values.Where(x=>  x.GetOrderingDate()!=null)
                                                      .OrderByDescending(x => x.GetOrderingDate())
                                                     .Skip(1).Take(window).ToList();
            
            //We ignore the last candle which is the current candle
            bool foundLower = false;
            foreach (MarketData md in candles)
            {
                if (Islower(md,lastCandle)
                    && md.GetOrderingDate().HasValue && lastCandle.GetOrderingDate().HasValue
                    && DateTime.Compare(md.GetOrderingDate().Value, lastCandle.GetOrderingDate().Value) < 0)
                {
                    foundLower = true;
                    break;
                }
            }

            return !foundLower;

        }

        #endregion

        #region Public Methods


        public override bool AppendCandleHistorical(MarketData md)//Historical Candles come w/right Open/Close/Trade
        {
            bool newCandle = false;
            if (CanCreateCandle(md))
            {
                string key = GetCandleKey(md);

                if (!Candles.ContainsKey(key))
                {

                    Candles.Add(key, md);
                }
                else
                {
                    Candles[key] = md;
                }

            }

            Security.MarketData = md;
            return newCandle;
        }

        public override bool AppendCandle(MarketData md)//Market Data candles come with yesterday Open/Close
        {
            bool newCandle = false;
            if (CanCreateCandle(md))
            {
                string key = GetCandleKey(md);

                if (!Candles.ContainsKey(key))
                {

                    MarketData lastCandle = GetLastFinishedCandle();
                    if(lastCandle!=null)
                        lastCandle.ClosingPrice = lastCandle.Trade;

                    newCandle = true;
                    md.OpeningPrice = md.Trade;
                    Candles.Add(key, md);
                    //The trade price is always the Close price of the candle
                    //The last candle will have a Trade and when switching to the next candle, that trade will be the close price
                }
                else
                {
                    Candles[key].Trade = md.Trade;
                }

            }
            
            Security.MarketData = md;
            return newCandle;
        }

        public virtual bool LongSignalTriggered()
        {
            bool isHighestTurtles = IsHighest(OpenWindow);
            bool higherMMov = IsHigherThanMMov(CloseWindow, false);

            if (isHighestTurtles && higherMMov)
            {

                LastSignalTriggered = $"LONG : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(CloseWindow)}";
                return true;
            }
            else
                return false;
        }
        
        public virtual bool ShortSignalTriggered()
        {

            bool isLowestTurtles = IsLowest(OpenWindow);
            bool higherMMov = IsHigherThanMMov(CloseWindow, false);

            if (isLowestTurtles && !higherMMov)
            {

                LastSignalTriggered = $"SHORT : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(CloseWindow)}";
                return true;
            }
            else
                return false;
            
        }
        
        public virtual bool EvalClosingLongPosition()
        {

            if (ExitOnMMov)
            {
                bool higherMMov = IsHigherThanMMov(CloseWindow, true);
                if (!higherMMov)
                {
                    LastSignalTriggered = $"CLOSE LONG w/MMov : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(CloseWindow)}";
                    return true;
                }
                else
                    return false;

            }
            else
            {
                bool isLowestTurtles = IsLowest(CloseWindow);
                if (isLowestTurtles)
                {
                    LastSignalTriggered = $"CLOSE LONG w/Turtles : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(CloseWindow)}";
                    return true;
                }
                else 
                { 
                
                    return false;
                }
                
            }
        }

        public virtual bool EvalClosingShortPosition()
        {
            if (ExitOnMMov)
            {
                bool higherMMov = IsHigherThanMMov(CloseWindow, true);
                if (higherMMov)
                {
                    LastSignalTriggered = $"CLOSE SHORT w/MMOV : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(CloseWindow)}";
                    return true;
                }
                else
                    return false;

            }
            else
            {
                bool isHighestTurtles = IsLowest(CloseWindow);
                if (isHighestTurtles)
                {
                    LastSignalTriggered = $"CLOSE SHORT w/Turtles : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(CloseWindow)}";
                    return true;
                }
                else
                {

                    return false;
                }
            }
        }
        
        
        public virtual List<MarketData> GetHistoricalPrices()
        {
            List<MarketData> histPrices = new List<MarketData>(Candles.Values);
            return histPrices.OrderBy(x => x.GetOrderingDate()).ToList();
        }
        
        public virtual MarketData GetLastFinishedCandle(int cowntdown)
        {
            return Candles.Values.OrderByDescending(x => x.GetOrderingDate()).ToArray()[cowntdown];
        }
        
        public virtual MarketData GetLastFinishedCandle()
        {
            //return Candles.Values.OrderByDescending(x => x.GetOrderingDate()).FirstOrDefault();
            return GetLastFinishedCandle(1);
        }

        public virtual MarketData GetCurrentCandle()
        {
            //return Candles.Values.OrderByDescending(x => x.GetOrderingDate()).FirstOrDefault();
            return GetLastFinishedCandle(0);
        }

        public virtual bool HasHistoricalCandles()
        {
            return Candles.Keys.Count > 0;
        }

        protected DateTime GetDateForCandle(MarketData md)
        {
            if (md != null && md.MDEntryDate.HasValue)
                return md.MDEntryDate.Value;
            if (md != null && md.MDLocalEntryDate.HasValue)
                return md.MDLocalEntryDate.Value;
            else
                return DateTimeManager.Now;
        }
        
        public virtual DateTime GetLastFinishedCandleDate()
        {
            MarketData lastCandle = GetLastFinishedCandle();

            return GetDateForCandle(lastCandle);
        }

        public virtual DateTime GetCurrentCandleDate()
        {
            MarketData lastCandle = GetCurrentCandle();

            return GetDateForCandle(lastCandle);
        }

        public override MarketData GetLastTriggerPrice()
        {
            return GetCurrentCandle();
        }


        public virtual bool EvalStopLossHit(TradTurtlesPosition tradPos)
        {

            if (tradPos.OpeningPosition != null && !tradPos.OpeningPosition.PositionRouting())
            {
                if (tradPos.IsFirstLeg() && tradPos.IsLongDirection())
                {
                    double pct = StopLossForOpenPositionPct / 100;
                    double closingPrice = tradPos.OpeningPosition.AvgPx.Value * (1 - pct);

                    return Security.MarketData.Trade < closingPrice;
                }
                else if (tradPos.IsFirstLeg() && tradPos.IsShortDirection())
                {
                    double pct = StopLossForOpenPositionPct / 100;
                    double closingPrice = tradPos.OpeningPosition.AvgPx.Value * (1 + pct);

                    return Security.MarketData.Trade > closingPrice;
                }
                else
                {
                    return false;
                }

            }
            else
            {
                return false;
            }
        }

        public virtual bool EvalAbortingClosingShortPosition()
        {
            return false;//When triggered, we just CLOSE the SHORT positions- 
        }

        public virtual bool EvalAbortingClosingLongPosition()
        {
            return false;//When triggered, we just CLOSE the LONG positions-
        }

        public bool EvalAbortingNewLongPosition()
        {
            return false;//When triggered, we just open the LONG positions-
        }

        public virtual bool EvalAbortingNewShortPosition()
        {
            return false;//When triggered, we just open the SHORT positions-
        }

        public virtual string SignalTriggered()
        {
            //It logs information abou the signal that has been triggered
            return LastSignalTriggered;

        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.DayTurtles.Common.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;
using static zHFT.Main.Common.Util.Constants;

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


        public MonTurtlePosition(TurtlesCustomConfig pTurtlesCustomConfig, double stopLossForOpenPositionPct, string candleRefPrice, string marketStartTime=null, string marketEndTime = null)
        {
            Candles = new Dictionary<string, MarketData>();
            TurtlesCustomConfig = pTurtlesCustomConfig;
            StopLossForOpenPositionPct = stopLossForOpenPositionPct;
            CandleReferencePrice = candleRefPrice;
            MarketStartTime = marketStartTime;
            MarketEndTime   = marketEndTime;


            MonitoringType = zHFT.Main.Common.Enums.MonitoringType.ONLY_ROUTING;

        }

        #endregion

        #region Protected Attributes

        public TurtlesCustomConfig TurtlesCustomConfig { get; set; }

        public double StopLossForOpenPositionPct { get; set; }



        protected string LastSignalTriggered { get; set; }

        protected string MarketStartTime { get; set; }


        protected string MarketEndTime { get; set; }

        protected string ClosingTime { get; set; }

        

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
            return ReferencePriceCalculator.GetReferencePrice(md, CandleReferencePrice) > ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice);

        }

        public bool Islower(MarketData md, MarketData lastCandle)
        {
            return ReferencePriceCalculator.GetReferencePrice(md, CandleReferencePrice) < ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice);

        }

        public double CalculateSimpleMovAvg()
        {
            return CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow);
        }

        public double CalculateSimpleMovAvg(int window, int skip = 0)
        {
            List<MarketData> windowcandles = Candles.Values.Where(x => x.GetOrderingDate() != null)
                                              .OrderByDescending(x => x.GetOrderingDate().Value)
                                              .Skip(skip).Take(window).ToList();

            //calculate the avg
            double sum = 0;
            int count = 0;
            foreach (MarketData md in windowcandles)
            {
                if (ReferencePriceCalculator.GetReferencePrice(md, CandleReferencePrice) != null)
                {
                    sum += ReferencePriceCalculator.GetReferencePrice(md, CandleReferencePrice).Value;
                    count++;
                }
            }

            double avg = sum / Convert.ToDouble(count);
            return avg;
        }

        public bool IsHigherThanMMov(int window, bool higherOrEqual)
        {
            MarketData lastCandle = LastValidCandle();

            double avg = CalculateSimpleMovAvg(window);
            DoLog($"DBG8.IV- IsLowest--> LastCandle.Date={lastCandle.GetOrderingDate()} Price={ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice)} avg={avg}", MessageType.Debug);

            if (ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice) != null)
            {
                if (higherOrEqual)
                    return ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice).Value >= avg;
                else
                    return ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice).Value > avg;

            }
            else
                return false;

        }

        public Dictionary<int, double> GetPrevMovAvgs(int window, int skip)
        {

            int i = skip;

            Dictionary<int, double> prevMovAvgs = new Dictionary<int, double>();

            while (i != -1)
            {

                double movAvg = CalculateSimpleMovAvg(window, i);
                prevMovAvgs.Add(i, movAvg);
                i--;
            }

            return prevMovAvgs;
        }


        public bool IsPositiveSlope(int window,int skip)
        {
            Dictionary<int, double> prevMovAvgs = GetPrevMovAvgs(window, skip);

            double? prevMovAvg = null;
            foreach (int key in prevMovAvgs.Keys.OrderByDescending(x => x))
            {

                if (!prevMovAvg.HasValue)
                {
                    prevMovAvg = prevMovAvgs[key];

                }
                else
                {
                    double currMovAvg = prevMovAvgs[key];

                    if (currMovAvg < prevMovAvg || double.IsNaN(currMovAvg))
                        return false;

                    prevMovAvg = currMovAvg;
                }
            }

            return true;
        
        }


        public bool IsNegativeSlope(int window, int skip)
        {
            Dictionary<int, double> prevMovAvgs = GetPrevMovAvgs(window, skip);

            double? prevMovAvg = null;
            foreach (int key in prevMovAvgs.Keys.OrderByDescending(x => x))
            {

                if (!prevMovAvg.HasValue)
                {
                    prevMovAvg = prevMovAvgs[key];

                }
                else
                {
                    double currMovAvg = prevMovAvgs[key];

                    if (currMovAvg > prevMovAvg || double.IsNaN(currMovAvg))
                        return false;

                    prevMovAvg = currMovAvg;
                }
            }

            return true;

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

            DoLog($"DBG8.IV- IsHighest--> LastCandle.Date={lastCandle.GetOrderingDate()} Price={ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice)}", MessageType.Debug);

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
                    DoLog($"DBG8.V- FoundBigger--> LastCandle.Date={md.GetOrderingDate()} Price={ReferencePriceCalculator.GetReferencePrice(md, CandleReferencePrice)}", MessageType.Debug);
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
            DoLog($"DBG8.IV- IsLowest--> LastCandle.Date={lastCandle.GetOrderingDate()} Price={ReferencePriceCalculator.GetReferencePrice(lastCandle, CandleReferencePrice)}", MessageType.Debug);

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
                    DoLog($"DBG8.V- FoundLower--> LastCandle.Date={md.GetOrderingDate()} Price={ReferencePriceCalculator.GetReferencePrice(md, CandleReferencePrice)}", MessageType.Debug);
                    break;
                }
            }
            
            return !foundLower;

        }

        public bool EvalSkippingFalseLong()
        {
            if (TurtlesCustomConfig.ExitOnTurtles)
            {

                bool isLowestTurtles = IsLowest(TurtlesCustomConfig.CloseWindow);
                DoLog($"Eval Skipping Long <turtles>--> isLowestTurtles={isLowestTurtles} for {Security.Symbol}", Constants.MessageType.Debug);
                return isLowestTurtles;

            }
            else if (TurtlesCustomConfig.ExitOnMMov)
            {
                bool isHigherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, true);
                DoLog($"Eval Skipping Long <mmov>--> isHigherMMov={isHigherMMov} for {Security.Symbol}", Constants.MessageType.Debug);
                return !isHigherMMov;
            }
            else
                return false;
        }

        public bool EvalSkippingFalseShort()
        {
            if (TurtlesCustomConfig.ExitOnTurtles)
            {

                bool isHighestTurtles = IsHighest(TurtlesCustomConfig.CloseWindow);
                DoLog($"Eval Skipping Short <turtles>--> isHighest={isHighestTurtles} for {Security.Symbol}", Constants.MessageType.Debug);
                return isHighestTurtles;

            }
            else if (TurtlesCustomConfig.ExitOnMMov)
            {
                bool isLowerMMov = !IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, true);
                DoLog($"Eval Skipping Short <mmov>--> isLowerMMov={isLowerMMov} for {Security.Symbol}", Constants.MessageType.Debug);
                return !isLowerMMov;
            }
            else
                return false;
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

        public bool AppendCandleLight(MarketData md)
        {
            string key = GetCandleKey(md);
            Candles[key] = md;
            return true;
        }

        public override bool AppendCandle(MarketData md)//Market Data candles come with yesterday Open/Close
        {
            DoLog($"@DBG_MD3.i- Eval Can Create Candle", MessageType.Debug);
            bool newCandle = false;
            if (CanCreateCandle(md))
            {
                string key = GetCandleKey(md);
                DoLog($"@DBG_MD3.i- Candle Key ={key}", MessageType.Debug);

                if (!Candles.ContainsKey(key))
                {

                    MarketData lastCandle = GetLastFinishedCandle();
                    if (lastCandle != null)
                    {
                        DoLog($"@DBG_MD3.i- Adding Candle Time={lastCandle.GetReferenceDateTime()} Close={lastCandle.ClosingPrice} Trade={lastCandle.Trade}", MessageType.Debug);
                        lastCandle.ClosingPrice = lastCandle.Trade;
                    }
                    DoLog($"@DBG_MD3.i - New Candle, opening prices is the last trade Key {key}: Trade={md.Trade}", MessageType.Debug);
                    newCandle = true;
                    md.OpeningPrice = md.Trade;
                    Candles.Add(key, md);
                    DoLog($"@DBG_MD3.i - New Candle - Candles.Count {Candles.Count}: Trade={md.Trade}", MessageType.Debug);

                    //The trade price is always the Close price of the candle
                    //The last candle will have a Trade and when switching to the next candle, that trade will be the close price
                }
                else
                {
                    DoLog($"@DBG_MD3.i - Updating current market price for Key {key}: Trade={md.Trade}", MessageType.Debug);
                    Candles[key].Trade = md.Trade;
                }

            }
            
            Security.MarketData = md;
            return newCandle;
        }


        public virtual bool LongSignalTriggered()
        {
            bool isHighestTurtles = IsHighest(TurtlesCustomConfig.OpenWindow);
            bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);

            if (isHighestTurtles && higherMMov)
            {

                LastSignalTriggered = $"LONG : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
                return false;
        }
        
        public virtual bool ShortSignalTriggered()
        {

            bool isLowestTurtles = IsLowest(TurtlesCustomConfig.OpenWindow);
            bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);

            if (isLowestTurtles && !higherMMov)
            {

                LastSignalTriggered = $"SHORT : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
                return false;
            
        }

        public virtual bool EvalClosingOnTargetPct(PortfolioPosition portfPos)
        {
            if (TurtlesCustomConfig.TakeProfitPct.HasValue)
            {
                DoLog($"DBG_MD - Evaluating Closing on Target Pct : Candle Count= {Candles.Count}", MessageType.Debug);
                MarketData lastCandle = GetCurrentCandle();
                decimal currProfit = portfPos.CalculateProfit(lastCandle);
                LastSignalTriggered = $"CLOSE Pos w/Target Profit: CurrProfit= {portfPos.CalculateProfit(lastCandle).ToString("#.##")}" +
                                      $"Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)}";
                return currProfit > TurtlesCustomConfig.TakeProfitPct.Value;

            }
            else
                return false;
        
        }
        
        public virtual bool EvalClosingLongPosition(PortfolioPosition portfPos)
        {
            if (!portfPos.IsLongDirection())
                return false;

            if (EvalClosingOnTargetPct(portfPos))
            {
                DoLog($"DBG8.I- Closing LONG on Tgt. Pct", MessageType.Debug);
                return true;
            }

            if (TurtlesCustomConfig.ExitOnMMov)
            {
                bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, true);
                if (!higherMMov)
                {
                    DoLog($"DBG8.II- Closing LONG on MMOV", MessageType.Debug);
                    LastSignalTriggered = $"CLOSE LONG w/MMov : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                    return true;
                }
                else
                    return false;

            }
            else if (TurtlesCustomConfig.ExitOnTurtles)
            {
                bool isLowestTurtles = IsLowest(TurtlesCustomConfig.CloseWindow);
                if (isLowestTurtles)
                {
                    DoLog($"DBG8.III- Closing LONG Lowest Turtles", MessageType.Debug);
                    LastSignalTriggered = $"CLOSE LONG w/Turtles : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                    return true;
                }
                else
                {
                    return false;
                }

            }
            else
            {
                throw new Exception($"No proper exit algo specified for symbol {Security.Symbol}");
            
            }
        }

        public virtual bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
            if (!portfPos.IsShortDirection())
                return false;

            if (EvalClosingOnTargetPct(portfPos))
            {
                DoLog($"DBG8.I- Closing SHORT On Tgt Pct.", MessageType.Debug);
                return true;
            }

            if (TurtlesCustomConfig.ExitOnMMov)
            {
                bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, true);
                if (higherMMov)
                {
                    DoLog($"DBG8.II- Closing SHORT On MMov", MessageType.Debug);
                    LastSignalTriggered = $"CLOSE SHORT w/MMOV : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                    return true;
                }
                else
                    return false;

            }
            else if(TurtlesCustomConfig.ExitOnTurtles)
            {
                bool isHighestTurtles = IsHighest(TurtlesCustomConfig.CloseWindow);
                if (isHighestTurtles)
                {
                    DoLog($"DBG8.III- Closing SHORT On Turtles", MessageType.Debug);
                    LastSignalTriggered = $"CLOSE SHORT w/Turtles : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(),CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                    return true;
                }
                else
                {

                    return false;
                }
            }
            else
            {
                throw new Exception($"No proper exit algo specified for symbol {Security.Symbol}");

            }
        }
        
        
        public virtual List<MarketData> GetHistoricalPrices()
        {
            List<MarketData> histPrices = new List<MarketData>(Candles.Values);
            return histPrices.OrderBy(x => x.GetOrderingDate()).ToList();
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
            //return GetCurrentCandle();
            return GetLastFinishedCandle();
        }


        public virtual bool EvalStopLossHit(PortfTurtlesPosition tradPos)
        {

            if (tradPos.OpeningPosition != null && !tradPos.OpeningPosition.PositionRouting())
            {
                if (tradPos.IsFirstLeg() && tradPos.IsLongDirection())// ex:At 15:02, we use 15:01 candle
                {
                    double pct = StopLossForOpenPositionPct / 100;
                    double closingPrice = tradPos.OpeningPosition.AvgPx.Value * (1 - pct);

                    return GetLastFinishedCandle().Trade < closingPrice;
                }
                else if (tradPos.IsFirstLeg() && tradPos.IsShortDirection())// ex:At 15:02, we use 15:01 candle
                {
                    double pct = StopLossForOpenPositionPct / 100;
                    double closingPrice = tradPos.OpeningPosition.AvgPx.Value * (1 + pct);

                    return GetLastFinishedCandle().Trade > closingPrice;
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
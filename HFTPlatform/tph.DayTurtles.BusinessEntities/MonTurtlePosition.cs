using System;
using System.Collections.Generic;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.DayTurtles.BusinessEntities
{
    public class MonTurtlePosition : PortfolioPosition
    {
        #region Constructors

        public MonTurtlePosition(int openWindow, int closeWindow,double stopLossForOpenPositionPct)
        {
            Candles = new Dictionary<string, MarketData>();
            OpenWindow = openWindow;
            CloseWindow = closeWindow;
            StopLossForOpenPositionPct = stopLossForOpenPositionPct;
        }

        #endregion

        #region Protected Attributes

        public int OpenWindow { get; set; }

        public int CloseWindow { get; set; }
        
        public double StopLossForOpenPositionPct { get; set; }

        public Dictionary<string, MarketData> Candles { get; set; }

        #endregion

        #region Private Methods

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

        public bool IsHighest(int window)
        {
            if (Candles.Count < window)
                return false;
            
            //We ignore the last candle which is the current candle
            List<string> keys = Candles.Keys.Skip(Candles.Count - window - 1).ToList();

            if (keys.Count == (window + 1))
            {
                bool higher = false;

                foreach (string key in keys)
                {
                    if (Candles[key].ClosingPrice > Security.MarketData.ClosingPrice 
                        && Candles[key].MDEntryDate.HasValue && Security.MarketData.MDEntryDate.HasValue
                        && DateTime.Compare(Candles[key].MDEntryDate.Value, Security.MarketData.MDEntryDate.Value) < 0)
                    {
                        higher = true;
                    }
                }

                return !higher;

            }
            else
                return false;
            
        }
        
        public bool IsLowest(int window)
        {
            if (Candles.Count < window)
                return false;
            
            //We ignore the last candle which is the current candle
            List<string> keys = Candles.Keys.Skip(Candles.Count - window - 1).ToList();

            if (keys.Count == (window + 1))
            {
                bool lower = false;

                foreach (string key in keys)
                {
                    if (Candles[key].ClosingPrice < Security.MarketData.ClosingPrice 
                        && Candles[key].MDEntryDate.HasValue && Security.MarketData.MDEntryDate.HasValue
                        && DateTime.Compare(Candles[key].MDEntryDate.Value, Security.MarketData.MDEntryDate.Value) < 0)
                    {
                        lower = true;
                    }
                }

                return !lower;

            }
            else
                return false;
            
        }

        #endregion
        
        #region Public Methods

        public void AppendCandle(MarketData md)
        {
            if (md.MDEntryDate.HasValue)
            {
                string key = md.MDEntryDate.Value.ToString("yyyyMMddhhmm");

                if (!Candles.ContainsKey(key))
                    Candles.Add(key, md);
                else
                    Candles[key] = md;

            }

            Security.MarketData = md;

        }

        public bool LongSignalTriggered()
        {
            return IsHighest(OpenWindow);
        }
        
        public bool ShortSignalTriggered()
        {
            return IsLowest(OpenWindow);
        }
        
        public bool EvalClosingLongPosition()
        {
            return IsLowest(CloseWindow);
        }

        public bool EvalClosingShortPosition()
        {
            return IsHighest(CloseWindow);
        }

        public bool EvalStopLossHit(TradTurtlesPosition tradPos)
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

        public bool EvalAbortingClosingShortPosition()
        {
            return false;//When triggered, we just CLOSE the SHORT positions- 
        }

        public bool EvalAbortingClosingLongPosition()
        {
            return false;//When triggered, we just CLOSE the LONG positions-
        }

        public bool EvalAbortingNewLongPosition()
        {
            return false;//When triggered, we just open the LONG positions-
        }

        public bool EvalAbortingNewShortPosition()
        {
            return false;//When triggered, we just open the SHORT positions-
        }

        #endregion
    }
}
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

        public MonTurtlePosition(int openWindow, int closeWindow,double stopLossForOpenPositionPct, string candleRefPrice)
        {
            Candles = new Dictionary<string, MarketData>();
            OpenWindow = openWindow;
            CloseWindow = closeWindow;
            StopLossForOpenPositionPct = stopLossForOpenPositionPct;
            CandleReferencePrice = candleRefPrice;
        }

        #endregion

        #region Protected Attributes

        public int OpenWindow { get; set; }

        public int CloseWindow { get; set; }
        
        public double StopLossForOpenPositionPct { get; set; }
        
        public string CandleReferencePrice { get; set; }

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

        public bool IsBigger(MarketData  md , MarketData lastCandle)
       {
           if (string.IsNullOrEmpty(CandleReferencePrice))
           {
               return md.ClosingPrice > lastCandle.ClosingPrice;
           }
           else if (CandleReferencePrice == _CANLDE_REF_PRICE_CLOSE)
           {
               return md.ClosingPrice > lastCandle.ClosingPrice;
           }
           else if (CandleReferencePrice == _CANLDE_REF_PRICE_TRADE)
           {
               return md.Trade > lastCandle.Trade;
           }
           else
           {
               throw new Exception(string.Format("Candle Reference  Price not recognized:{0}",CandleReferencePrice));
           }
       }

        public bool Islower(MarketData  md , MarketData lastCandle)
        {
            if (string.IsNullOrEmpty(CandleReferencePrice))
            {
                return md.ClosingPrice < lastCandle.ClosingPrice;
            }
            else if (CandleReferencePrice == _CANLDE_REF_PRICE_CLOSE)
            {
                return md.ClosingPrice < lastCandle.ClosingPrice;
            }
            else if (CandleReferencePrice == _CANLDE_REF_PRICE_TRADE)
            {
                return md.Trade < lastCandle.Trade;
            }
            else
            {
                throw new Exception(string.Format("Candle Reference  Price not recognized:{0}",CandleReferencePrice));
            }
        }
        
        public bool IsHighest(int window)
        {
            if (Candles.Count < window)
                return false;

            MarketData lastCandle = Candles.Values.OrderByDescending(x => x.MDEntryDate.Value).FirstOrDefault();

            List<MarketData> candles = Candles.Values.OrderByDescending(x => x.MDEntryDate.Value)
                                                      .Skip(1).Take(window).ToList();
            
            //We ignore the last candle which is the current candle
            bool foundBigger = false;
            foreach (MarketData md in candles)
            {
                if ( 
                    IsBigger(md,lastCandle)
                    && md.MDEntryDate.HasValue && lastCandle.MDEntryDate.HasValue
                    && DateTime.Compare(md.MDEntryDate.Value, lastCandle.MDEntryDate.Value) < 0)
                {
                    foundBigger = true;
                }
            }

            return !foundBigger;
            
        }
        
        public bool IsLowest(int window)
        {
            if (Candles.Count < window)
                return false;

            MarketData lastCandle = Candles.Values.OrderByDescending(x => x.MDEntryDate.Value).FirstOrDefault();

            List<MarketData> candles = Candles.Values.OrderByDescending(x => x.MDEntryDate.Value)
                                                     .Skip(1).Take(window).ToList();
            
            //We ignore the last candle which is the current candle
            bool foundLower = false;
            foreach (MarketData md in candles)
            {
                if (Islower(md,lastCandle)
                    && md.MDEntryDate.HasValue && lastCandle.MDEntryDate.HasValue
                    && DateTime.Compare(md.MDEntryDate.Value, lastCandle.MDEntryDate.Value) < 0)
                {
                    foundLower = true;
                }
            }

            return !foundLower;

        }

        #endregion
        
        #region Public Methods

        public virtual bool AppendCandle(MarketData md)
        {
            bool newCandle = false;
            if (md.MDEntryDate.HasValue)
            {
                string key = md.MDEntryDate.Value.ToString("yyyyMMddHHmm");
                
                if (!Candles.ContainsKey(key))
                {
                    newCandle = true;
                    Candles.Add(key, md);
                }
                else
                    Candles[key] = md;

            }
            
            Security.MarketData = md;
            return newCandle;
        }

        public virtual bool LongSignalTriggered()
        {
            return IsHighest(OpenWindow);
        }
        
        public virtual bool ShortSignalTriggered()
        {
            return IsLowest(OpenWindow);
        }
        
        public virtual bool EvalClosingLongPosition()
        {
            return IsLowest(CloseWindow);
        }

        public virtual bool EvalClosingShortPosition()
        {
            return IsHighest(CloseWindow);
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
            return "";

        }

        #endregion
    }
}
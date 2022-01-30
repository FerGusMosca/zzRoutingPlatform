using System;
using System.Collections.Generic;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class ImbalancePositionTurtlesExit:ImbalancePosition
    {
        
        #region Public Static Consts

        public static string _CANLDE_REF_PRICE_TRADE = "TRADE";
        public static string _CANLDE_REF_PRICE_CLOSE = "CLOSE";
        
        #endregion
        
        #region Protected Methods
        
        public bool IsBigger(MarketData  md , MarketData lastCandle, string CandleReferencePrice)
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

        public bool Islower(MarketData  md , MarketData lastCandle, string CandleReferencePrice)
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
        
        public bool IsHighest(int window,Dictionary<string, MarketData> Candles, string CandleReferencePrice)
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
                    IsBigger(md,lastCandle,CandleReferencePrice)
                    && md.MDEntryDate.HasValue && lastCandle.MDEntryDate.HasValue
                    && DateTime.Compare(md.MDEntryDate.Value, lastCandle.MDEntryDate.Value) < 0)
                {
                    foundBigger = true;
                }
            }

            return !foundBigger;
            
        }
        
        public bool IsLowest(int window,Dictionary<string, MarketData> Candles, string CandleReferencePrice)
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
                if (Islower(md,lastCandle,CandleReferencePrice)
                    && md.MDEntryDate.HasValue && lastCandle.MDEntryDate.HasValue
                    && DateTime.Compare(md.MDEntryDate.Value, lastCandle.MDEntryDate.Value) < 0)
                {
                    foundLower = true;
                }
            }

            return !foundLower;

        }

        #endregion
        
        #region Public Overriden Methods

        public override bool EvalClosingShortPosition(SecurityImbalance secImb,
            decimal positionOpeningImbalanceMaxThreshold)
        {

            if (secImb is SecurityImbalanceTurtlesExit)
            {
                SecurityImbalanceTurtlesExit tSecImb = (SecurityImbalanceTurtlesExit) secImb;

                bool newHigh = IsHighest(tSecImb.CloseWindow, tSecImb.Candles,tSecImb.CandleReferencePrice);
                
                return (TradeDirection == ImbalancePosition._SHORT 
                        && !secImb.Closing
                       && newHigh
                       && (OpeningPosition.PosStatus == PositionStatus.Filled || OpeningPosition.PosStatus == PositionStatus.PartiallyFilled));


                return true;
            }
            else
            {
                throw new Exception(string.Format(
                    "Critical ERROR @ImbalancePositionTurtlesExit.EvalClosingShortPosition: secImb parameter must be of SecurityImbalanceTurtlesExit type"));
            }
        }
        
        public override bool EvalClosingLongPosition(SecurityImbalance secImb, decimal positionOpeningImbalanceMaxThreshold)
        {

            if (secImb is SecurityImbalanceTurtlesExit)
            {
                SecurityImbalanceTurtlesExit tSecImb = (SecurityImbalanceTurtlesExit) secImb;
                
                bool newLow = IsLowest(tSecImb.CloseWindow, tSecImb.Candles,tSecImb.CandleReferencePrice);
                
                return (TradeDirection == ImbalancePosition._LONG
                        && !secImb.Closing
                        && newLow
                        && (OpeningPosition.PosStatus == PositionStatus.Filled ||
                            OpeningPosition.PosStatus == PositionStatus.PartiallyFilled));
            }
            else
            {
                throw new Exception(string.Format(
                    "Critical ERROR @ImbalancePositionTurtlesExit.EvalClosingLongPosition: secImb parameter must be of SecurityImbalanceTurtlesExit type"));
            }
        }
        
        public override bool EvalAbortingClosingLongPosition(SecurityImbalance secImb, decimal positionOpeningImbalanceMaxThreshold)
        {
            return false; //Turtles dont abort closing /opening position
        }
        
        public override bool EvalAbortingClosingShortPosition(SecurityImbalance secImb, decimal positionOpeningImbalanceMaxThreshold)
        {
            return false;//Turtles dont abort closing /opening position
        }
        
        public override bool EvalAbortingNewLongPosition(SecurityImbalance secImb, decimal PositionOpeningImbalanceThreshold)
        {
            return false;//Turtles dont abort closing /opening position
        }

        public override bool EvalAbortingNewShortPosition(SecurityImbalance secImb, decimal PositionOpeningImbalanceThreshold)
        {
            return false;//Turtles dont abort closing /opening position
        }
        
        #endregion
        
    }
}
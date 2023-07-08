using System;
using System.Collections.Generic;
using System.Linq;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.IntFig.DayTurtles.BusinessEntities
{
    public class MonIntFigTurtlePosition: MonTrendlineTurtlesPosition
    {
        
        #region Public Attributes
        
        public double ProximityPctToTriggerTrade { get; set; }
        
        public Trendline LastTrendlineTouched { get; set; }
        
        public MarketData LastSignalMarketData { get; set; }
        
        #endregion
        
        #region Constructors

        public MonIntFigTurtlePosition(int openWindow, int closeWindow, double stopLossForOpenPositionPct,
            int outerSignalSpan) : base(openWindow, closeWindow, stopLossForOpenPositionPct,
            PortfolioPosition._CANLDE_REF_PRICE_CLOSE)
        {
            Resistances = new List<Trendline>();
            Supports = new List<Trendline>();
            OuterSignalSpan = outerSignalSpan;
        }

        #endregion
        
        #region Public Methods

        public override bool LongSignalTriggered()
        {
            List<MarketData> histPrices = GetHistoricalPrices();
            MarketData lastClosedCandle = GetLastFinishedCandle();
            List<Trendline> activeSupports = Supports.Where(x => x.TrendlineType == TrendlineType.Support
                                                                 && !x.IsBroken(lastClosedCandle.MDEntryDate)
                                                                 && x.ValidDistanceToEndDate(lastClosedCandle.MDEntryDate.Value,OuterSignalSpan,CandleInterval.Minute_1)).ToList();

            foreach (Trendline trendline in activeSupports)
            {
                double trendlinePrice = trendline.CalculateTrendPrice(lastClosedCandle.MDEntryDate.Value, histPrices);

                double signalLevel = trendlinePrice * (1 + (ProximityPctToTriggerTrade / 100));

                if (signalLevel > lastClosedCandle.ClosingPrice)
                {
                    LastTrendlineTouched = trendline;
                    LastSignalMarketData = lastClosedCandle;
                    return true;
                }
            }

            return false;
        }
        
        public override bool EvalClosingLongPosition()
        {
            return IsLowest(CloseWindow) && !LongSignalTriggered();
            // If we have a long signal triggered , we stay in
        }
        
        public override bool ShortSignalTriggered()
        {
            return false;//TODO develop short positions
        }
        
        public override string SignalTriggered()
        {

            if (LastTrendlineTouched != null)
            {
                List<MarketData> histPrices = GetHistoricalPrices();
                double trendlinePrice = LastTrendlineTouched.CalculateTrendPrice(LastSignalMarketData.MDEntryDate.Value, histPrices);
                return string.Format(" --> Broken Resistance: Start={0} End={1} Now={2} LastCandlePrice={3} LastCandleDate={4}   ",
                    LastTrendlineTouched.StartDate, LastTrendlineTouched.EndDate,DateTime.Now,LastSignalMarketData.ClosingPrice,LastSignalMarketData.MDEntryDate.Value,
                                    trendlinePrice);
            }
            else
            {
                return "";
            }

        }

        
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.LogicLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.BOBDayTurtles.BusinessEntities
{
    public class MonBOBTurtlePosition : MonTrendlineTurtlesPosition
    {
        
        #region Constructors

        public MonBOBTurtlePosition(TurtlesCustomConfig pTurtlesCustomWindow, double stopLossForOpenPositionPct,
            int outerSignalSpan,string candleReferencePrice) : base(pTurtlesCustomWindow, stopLossForOpenPositionPct,candleReferencePrice)
        {
            Resistances = new List<Trendline>();
            Supports = new List<Trendline>();
            OuterSignalSpan = outerSignalSpan;
        }

        #endregion

        #region Public Methods

        public override bool LongSignalTriggered()
        {
            return EvalResistanceBroken() && IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
            
        }
        
        public override bool ShortSignalTriggered()
        {
            return EvalSupportBroken() && !IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
        }
        
        public override string SignalTriggered()
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
                    return string.Format(" --> Broken Resistance: Start={0} End={1} Now={2} LastCandlePrice={3} LastCandleDate={4} TrendlinePrice={5}  ",
                                        resistance.StartDate, resistance.EndDate, DateTimeManager.Now, lastCandle.Trade, lastCandle.MDEntryDate.Value,
                                        trendlinePrice);
                }
                else
                    return "NO SIGNAL- NO CANDLES";
            }
            
            else if (support != null)
            {
                MarketData lastCandle = GetLastFinishedCandle();
                if (lastCandle != null)
                {
                    List<MarketData> histPrices = GetHistoricalPrices();
                    double trendlinePrice = support.CalculateTrendPrice(lastCandle.MDEntryDate.Value, histPrices);
                    return string.Format(" --> Broken Support: Start={0} End={1} Now={2} LastCandlePrice={3} LastCandleDate={4} TrendlinePrice={5}  ",
                        support.StartDate, support.EndDate, DateTimeManager.Now, lastCandle.Trade, lastCandle.MDEntryDate.Value,
                        trendlinePrice);
                }
                else
                    return "NO SIGNAL- NO CANDLES";
            }
            else
            {
                return "";
            }

        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using tph.BOBDayTurtles.BusinessEntities;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.IntFig.DayTurtles.BusinessEntities
{
    public class MonIntFigTurtlePosition: MonTrendlineTurtlesPosition
    {
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
            //TODO eval cierta distancia del soporte
            return !EvalSupportBroken();
            
        }
        
        public override bool ShortSignalTriggered()
        {
            //TODO eval cierta distancia del soporte
            return !EvalResistanceBroken();
        }
        
        public override string SignalTriggered()
        {
            //It logs information abou the signal that has been triggered
            
            //TODO --> Record signal triggered
            return "";

//            Trendline resistance = Resistances.Where(x => x.JustBroken).FirstOrDefault();
//            Trendline support = Supports.Where(x => x.JustBroken).FirstOrDefault();
//
//            if (resistance != null)
//            {
//                MarketData lastCandle = GetLastCandle();
//                List<MarketData> histPrices = GetHistoricalPrices();
//                double trendlinePrice = resistance.CalculateTrendPrice(lastCandle.MDEntryDate.Value, histPrices);
//                return string.Format(" --> Broken Resistance: Start={0} End={1} Now={2} LastCandlePrice={3} LastCandleDate={4} TrendlinePrice={5}  ",
//                                    resistance.StartDate, resistance.EndDate,DateTime.Now,lastCandle.ClosingPrice,lastCandle.MDEntryDate.Value,
//                                    trendlinePrice);
//            }
//            
//            else if (support != null)
//            {
//                MarketData lastCandle = GetLastCandle();
//                List<MarketData> histPrices = GetHistoricalPrices();
//                double trendlinePrice = support.CalculateTrendPrice(lastCandle.MDEntryDate.Value, histPrices);
//                return string.Format(" --> Broken Support: Start={0} End={1} Now={2} LastCandlePrice={3} LastCandleDate={4} TrendlinePrice={5}  ",
//                    support.StartDate, support.EndDate,DateTime.Now,lastCandle.ClosingPrice,lastCandle.MDEntryDate.Value,
//                    trendlinePrice);
//            }
//            else
//            {
//                return "";
//            }

        }

        
        #endregion
    }
}
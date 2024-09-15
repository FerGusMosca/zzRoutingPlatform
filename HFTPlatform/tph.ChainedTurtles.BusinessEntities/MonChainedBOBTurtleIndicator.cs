using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;
using static zHFT.Main.Common.Util.Constants;

namespace tph.ChainedTurtles.BusinessEntities
{
    //SHORT Signal on downside breakthroughs
    //LONG Signal on upside breakthroughs
    public class MonChainedBOBTurtleIndicator: MonChainedTurtleIndicator, ITrendlineIndicator
    {
        #region Constructor 

        public MonChainedBOBTurtleIndicator(Security pSecurity,
                                                  TurtlesCustomConfig pTurtlesCustomConfig,
                                                    string pCode,
                                                   ILogger pLogger) :base(pSecurity,pTurtlesCustomConfig,pCode)
        {
            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);
        }

        #endregion

     

        #region Protected Methods

     

        #endregion

        #region Public Overriden Methods


        //Activates the signal indicator or other statistical calculations depending the monitoring position role in the trading strategy
        public override bool EvalSignalTriggered()
        {
            bool longSignal=LongSignalTriggered();
            bool shortSignal=ShortSignalTriggered();

           
            return longSignal || shortSignal;
        }

        public override bool LongSignalTriggered()
        {
            DoLog($"DBG3-LongSignalOn?={LongSignalOn} for sec {Security.Symbol}", MessageType.Debug);
            if (!LongSignalOn)
            {
                if (UpsideBreaktrhough())
                {
                    LongSignalOn = true;
                    LastSignalTriggered = $"OPEN LONG w/Resistance broken: Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} ";
                    LastSignalTimestamp = DateTimeManager.Now;
                    return LongSignalOn;
                }
                else
                {
                    DoLog($"DBG4-NO upside breakthrough for sec {Security.Symbol}", MessageType.Debug);
                    LastSignalTriggered = "";
                    return false;

                }
            }
            else
            {
                EvalTimestampExpiration();
                return LongSignalOn;
            }
        }

        public override bool ShortSignalTriggered()
        {
            DoLog($"DBG3-LongSignalOn?={LongSignalOn} for sec {Security.Symbol}", MessageType.Debug);
            if (!ShortSignalOn)
            {

                if (DownsideBreaktrhough())
                {
                    ShortSignalOn = true;
                    LastSignalTriggered = $"OPEN SHORT w/Support broken: Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} ";
                    LastSignalTimestamp = DateTimeManager.Now;
                    return ShortSignalOn;
                }
                else
                {
                    DoLog($"DBG4-NO downside breakthrough for sec {Security.Symbol}", MessageType.Debug);
                    LastSignalTriggered = "";
                    return false;
                }
            }
            else
            {
                EvalTimestampExpiration();
                return ShortSignalOn;
            }
        }

        public override bool IsTrendlineMonPosition()
        {
            return true;
        }

        public override bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
            bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
            if (higherMMov)
            {
                LastSignalTriggered = $"CLOSE SHORT w/MMov : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
                return false;
        }

        public override bool EvalClosingLongPosition(PortfolioPosition portfPos)
        {
            bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
            if (!higherMMov)
            {
                LastSignalTriggered = $"CLOSE LONG w/Turtles : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
            {

                return false;
            }
        }

        public override string SignalTriggered()
        {
            return LastSignalTriggered;
        }

        #endregion

        #region ITrendlineIndicator


        public int GetInnerTrendlinesSpan()
        {
            return InnerTrendlinesSpan;
        }

        public int GetOutterTrendlinesSpan()
        {
            return OutterTrendlinesSpan;
        }

        public double GetPerforationThreshold()
        {
            return PerforationThreshold;
        }

        public string GetCandleReferencePrice()
        {
            return CandleReferencePrice;
        }

        public bool GetRecalculateTrendlines()
        {
            return RecalculateTrendlines;
        }


        public int GetHistoricalPricesPeriod()
        {
            return HistoricalPricesPeriod;

        }

        public int GetSkipCandlesToBreakTrndln()
        {
            return SkipCandlesToBreakTrndln;

        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.ChainedTurtles.BusinessEntities
{
    //SHORT Signal on downside breakthroughs
    //LONG Signal on upside breakthroughs
    public class MonChainedBOBTurtleIndicator: MonChainedTurtleIndicator
    {
        #region Constructor 

        public MonChainedBOBTurtleIndicator(Security pSecurity,
                                                  TurtlesCustomConfig pTurtlesCustomConfig,
                                                    string pCode,
                                                   ILogger pLogger) :base(pSecurity,pTurtlesCustomConfig,pCode)
        {
           
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
            if (!LongSignalOn)
            {
                if (UpsideBreaktrhough())
                {
                    LongSignalOn = true;
                    LastSignalTimestamp = DateTimeManager.Now;
                    return LongSignalOn;
                }
                else
                {
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

            if (!ShortSignalOn)
            {

                if (DownsideBreaktrhough())
                {
                    ShortSignalOn = true;
                    LastSignalTimestamp = DateTimeManager.Now;
                    return ShortSignalOn;
                }
                else
                {
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

        #endregion
    }
}

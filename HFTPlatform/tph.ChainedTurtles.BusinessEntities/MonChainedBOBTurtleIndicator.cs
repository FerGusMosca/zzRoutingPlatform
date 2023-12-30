using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Util;

namespace tph.ChainedTurtles.BusinessEntities
{
    //SHORT Signal on downside breakthroughs
    //LONG Signal on upside breakthroughs
    public class MonChainedBOBTurtleIndicator: MonChainedTurtleIndicator
    {
        #region Constructor 

        public MonChainedBOBTurtleIndicator(Security pSecurity,
                                                  TurtlesCustomConfig pTurtlesCustomConfig,
                                                    string candleRefPrice,
                                                    string pCode,
                                                    string signalType,
                                                    bool reqMarketData):base(pSecurity,pTurtlesCustomConfig,candleRefPrice,pCode,signalType,reqMarketData)
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
                if (DownsideBreaktrhough())
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

                if (UpsideBreaktrhough())
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

        #endregion
    }
}

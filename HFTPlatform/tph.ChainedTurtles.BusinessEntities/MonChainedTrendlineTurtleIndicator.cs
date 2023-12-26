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
    public class MonChainedTrendlineTurtleIndicator: MonChainedTurtleIndicator
    {
        #region Constructor 

        public MonChainedTrendlineTurtleIndicator(Security pSecurity,
                                                  TurtlesCustomConfig pTurtlesCustomConfig,
                                                    string candleRefPrice,
                                                    string signalType,
                                                    bool reqMarketData):base(pSecurity,pTurtlesCustomConfig,candleRefPrice,signalType,reqMarketData)
        {
            LongSignalOn = false;
            ShortSignalOn = false;

            LastSignalTimestamp = null;
        }

        #endregion

        #region Protected Static Consts

        protected static int _SIGNAL_EXPIRATION_IN_MIN = 5;

        #endregion

        #region Protected Attributes

        protected bool LongSignalOn { get; set; }

        protected bool ShortSignalOn { get; set; }

        protected DateTime? LastSignalTimestamp { get; set; }

        #endregion

        #region Protected Methods

        public void EvalTimestampExpiration()
        {

            if (LastSignalTimestamp.HasValue)
            {
                TimeSpan elapsed = DateTimeManager.Now - LastSignalTimestamp.Value;

                if (elapsed.TotalMinutes > _SIGNAL_EXPIRATION_IN_MIN)
                {
                    LastSignalTimestamp = null;
                    LongSignalOn = false;
                    ShortSignalOn = false;
                }
            }
        
        }

        #endregion

        #region Public Overriden Methods


        //Activates the signal indicator or other statistical calculations depending the monitoring position role in the trading strategy
        public override void EvalSignalTriggered()
        {
            LongSignalTriggered();
            ShortSignalTriggered();

        }

        public override bool LongSignalTriggered()
        {
            if (!LongSignalOn)
            {
                if (EvalResistanceBroken() && IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false))
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
                if (EvalSupportBroken() && !IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false))
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

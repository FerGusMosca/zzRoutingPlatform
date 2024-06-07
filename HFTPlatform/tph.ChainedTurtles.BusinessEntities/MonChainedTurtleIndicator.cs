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
    public class MonChainedTurtleIndicator : MonTrendlineTurtlesPosition
    {

        #region Public Attributes

        public string Code { get; set; }

        public string SignalType { get; set; }

        public bool ReqPrices { get; set; }

        protected bool LongSignalOn { get; set; }

        protected bool ShortSignalOn { get; set; }

        protected DateTime? LastSignalTimestamp { get; set; }

        #endregion

        #region Protected Consts

        public static string _BOB_SIGNAL_TYPE = "BOB";
        public static string _BOB_INV_SIGNAL_TYPE = "BOB_INV";

        protected static int _SIGNAL_EXPIRATION_IN_MIN = 5;

        #endregion



        #region Constructor 

        public MonChainedTurtleIndicator(Security pSecurity, TurtlesCustomConfig pTurtlesCustomConfig,
                                        string candleRefPrice,string pCode,string signalType,bool reqPrices, 
                                        string marketStartTime=null,string marketEndTime=null) :base(pTurtlesCustomConfig, 0,candleRefPrice,marketStartTime,marketEndTime)
        {


            Security = pSecurity;
            Code = pCode;
            SignalType = signalType;
            ReqPrices = reqPrices;

            LongSignalOn = false;
            ShortSignalOn = false;
            LastSignalTimestamp = null;


        }

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

        public bool DownsideBreaktrhough()
        {
            return EvalSupportBroken() && !IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
        }

        public bool UpsideBreaktrhough()
        {
            return EvalResistanceBroken() && IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using zHFT.Main.Common.Util;

namespace tph.ChainedTurtles.Common.Util
{
    public class TurtleIndicatorBaseConfigLoader
    {
        #region Public Static Methods

        protected static void EvalTime(string time)
        {

            try
            {
                DateTime extrTime = MarketTimer.GetTodayDateTime(time);
                //If we got here, it worked ok
            }
            catch (Exception ex)
            {
                throw new Exception($"The following time is not properly formatted: {time}");
            }

        }

        public static string GetMarketStartTime( TurtleIndicatorBaseConfigDTO configDto)
        {
            if (!string.IsNullOrEmpty(configDto.marketStartTime))
            {
                EvalTime(configDto.marketStartTime);
                return configDto.marketStartTime;
            }
            else
                throw new Exception("Missing config value marketStartTime");


        }

        public static string GetMarketEndTime(TurtleIndicatorBaseConfigDTO configDto)
        {
            if (!string.IsNullOrEmpty(configDto.marketEndTime))
            {
                EvalTime(configDto.marketEndTime);
                return configDto.marketEndTime;
            }
            else
                throw new Exception("Missing config value marketEndTime");

        }

        public static string GetClosingTime(TurtleIndicatorBaseConfigDTO configDto)
        {
            if (!string.IsNullOrEmpty(configDto.closingTime))
            {
                EvalTime(configDto.closingTime);
                return configDto.closingTime;
            }
            else
                throw new Exception("Missing config value closingTime");

        }

        public static int GetHistoricalPricesPeriod(TurtleIndicatorBaseConfigDTO configDto)
        {
            if (configDto.historicalPricesPeriod.HasValue && configDto.historicalPricesPeriod.Value <= 0)
                return configDto.historicalPricesPeriod.Value;
            else
                throw new Exception("config value historicalPricesPeriod must be lower than 0");

        }


        public static int GetHistoricalPricesPeriod(TurtleIndicatorBaseConfigDTO configDto, int def)
        {
            if (configDto.historicalPricesPeriod.HasValue && configDto.historicalPricesPeriod.Value <= 0)
                return configDto.historicalPricesPeriod.Value;
            else
                return def;

        }


        public static bool GetRequestHistoricalPrices(TurtleIndicatorBaseConfigDTO configDto, bool def)
        {

            if (configDto.requestHistoricalPrices.HasValue)
                return configDto.requestHistoricalPrices.Value;
            else
                return true;
        }


        #endregion
    }
}

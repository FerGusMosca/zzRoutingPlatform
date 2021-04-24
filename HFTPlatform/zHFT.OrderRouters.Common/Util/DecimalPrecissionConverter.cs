using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net.Objects.Spot.MarketData;
using zHFT.Main.BusinessEntities.Positions;

namespace zHFT.OrderRouters.Common.Util
{
    public class DecimalPrecissionConverter
    {
        private static int GetDecimalPlaces(decimal decimalNumber)
        {
            if (decimalNumber % 1 == 0)
                return 0;
        
            int decimalPlaces = 1;
            decimal powers = 10.0m;
            if (decimalNumber > 0.0m)
            {
                while ((decimalNumber * powers) % 1 != 0.0m)
                {
                    powers *= 10.0m;
                    ++decimalPlaces;
                }
            }
        
            return decimalPlaces;
        }
        
        public static int GetDecimalPrecission(Position pos)
        {
            int countBid = 0, countAsk = 0;

            if (pos.Security.MarketData.BestBidCashSize.HasValue)
            {
                decimal num = pos.Security.MarketData.BestBidCashSize.Value;
                countBid = GetDecimalPlaces(num);
                //countBid = Math.Max(0, num.ToString().Length - Math.Truncate(num).ToString().Length - 1);
                //countBid = BitConverter.GetBytes(decimal.GetBits(pos.Security.MarketData.BestBidCashSize.Value)[3])[2];
            }

            if (pos.Security.MarketData.BestAskCashSize.HasValue)
            {
                decimal num = pos.Security.MarketData.BestAskCashSize.Value;
                countAsk = GetDecimalPlaces(num);
                //countAsk = Math.Max(0, num.ToString().Length - Math.Truncate(num).ToString().Length - 1);
                //countAsk = BitConverter.GetBytes(decimal.GetBits(pos.Security.MarketData.BestAskCashSize.Value)[3])[2];
            }

            int countMax = countBid > countAsk ? countBid : countAsk;

            return countMax;
        }

    }
}

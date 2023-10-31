using System;

namespace zHFT.InstructionBasedMarketClient.Binance.Common.Util
{
    public class EpochConverter
    {
        public static DateTime FromEpoch(long epoch)
        {
            DateTimeOffset dateTimeOffset2 = DateTimeOffset.FromUnixTimeMilliseconds(epoch);

            return dateTimeOffset2.DateTime;
        }
    }
}
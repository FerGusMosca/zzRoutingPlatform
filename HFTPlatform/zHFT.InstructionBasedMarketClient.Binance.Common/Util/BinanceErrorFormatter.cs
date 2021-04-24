using System;

namespace zHFT.InstructionBasedMarketClient.Binance.Common.Util
{
    public class BinanceErrorFormatter
    {
        public static string ProcessErrorMessage(Exception ex)
        {
            string error = ex.Message;

            if (ex.InnerException != null)
                error += " " + ex.InnerException.Message;

            return error;
        }
    }
}
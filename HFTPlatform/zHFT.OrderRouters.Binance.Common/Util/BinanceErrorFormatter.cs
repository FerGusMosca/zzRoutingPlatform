using System;

namespace zHFT.OrderRouters.BINANCE.Common.Util
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
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.BINANCE.Common.Util
{
    public class BinanceClientProxy : BinanceClient
    {
        #region Constructors

        public BinanceClientProxy(ApiClient pApiClient)
            : base(pApiClient)
        { 
        }

        #endregion

        #region Private Methods

        private string GetMaxDecimals(int decimalPrecission)
        {
            string decimals = "";

            for (int i = 0; i < decimalPrecission; i++)
                decimals += "#";

            return decimals;
        }

        private decimal TruncateDecimal(decimal d, int decimals)
        {
            if (decimals < 0)
                throw new ArgumentOutOfRangeException("decimals", "Value must be in range 0-28.");
            else if (decimals > 28)
                throw new ArgumentOutOfRangeException("decimals", "Value must be in range 0-28.");
            else if (decimals == 0)
                return Math.Truncate(d);
            else
            {
                decimal integerPart = Math.Truncate(d);
                decimal scalingFactor = d - integerPart;
                decimal multiplier = Convert.ToDecimal(Math.Pow(10, decimals));

                scalingFactor = Math.Truncate(scalingFactor * multiplier) / multiplier;

                return integerPart + scalingFactor;
            }
        }

        #endregion

        #region Public Methods

        public async Task<NewOrder> PostNewLimitOrder(string symbol, decimal quantity, decimal price, OrderSide side,int decimalPrecission)
        {

            quantity = TruncateDecimal(quantity, decimalPrecission);

            string qty = quantity.ToString("0." + GetMaxDecimals(decimalPrecission));
            string strPrice = price.ToString("0.########");

            var args = string.Format("symbol={0}&side={1}&type={2}&quantity={3}&timeInForce={4}&price={5}&recvWindow=5000",
                                      symbol, side == OrderSide.BUY ? "BUY" : "SELL", "LIMIT", qty, "GTC", strPrice);

            //var args = string.Format("symbol={0}&side={1}&type={2}&quantity={3}&timeInForce={4}&price={5}&recvWindow=5000",
            //                          "ETHBTC", "BUY", "LIMIT", "0.017", "GTC", "0.097444");

            var result = await _apiClient.CallAsync<NewOrder>(ApiMethod.POST, EndPoints.NewOrder, true, args);

            return result;
        }

        #endregion
    }
}

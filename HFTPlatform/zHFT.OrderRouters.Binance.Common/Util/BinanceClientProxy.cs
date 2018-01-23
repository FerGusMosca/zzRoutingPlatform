using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Account;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Utils;
using System;
using System.Collections.Generic;
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

        #region Public Methods

        public async Task<NewOrder> PostNewLimitOrder(string symbol, decimal quantity, decimal price, 
                                                      OrderSide side)
        {
            //Validates that the order is valid.
            //base.ValidateOrderValue(symbol, orderType, price, quantity, icebergQty);

            //var args = $"symbol={symbol.ToUpper()}&side={side}&type={orderType}&quantity={quantity}"
            //    + (orderType == OrderType.LIMIT ? $"&timeInForce={timeInForce}" : "")
            //    + (orderType == OrderType.LIMIT ? $"&price={price}" : "")
            //    + (icebergQty > 0m ? $"&icebergQty={icebergQty}" : "")
            //    + $"&recvWindow={recvWindow}";

            var args = string.Format("symbol={0}&side={1}&type={2}&quantity={3}&timeInForce={4}&price={5}&recvWindow=5000",
                                      symbol, "BUY", "LIMIT", "0.01", "GTC", "0.01");

            var result = await _apiClient.CallAsync<NewOrder>(ApiMethod.POST, EndPoints.NewOrder, true, args);

            return result;
        }

        #endregion
    }
}

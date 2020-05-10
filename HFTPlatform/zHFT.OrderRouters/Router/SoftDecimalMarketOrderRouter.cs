using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.OrderRouters.Common.Util;

namespace zHFT.OrderRouters.Router
{
    public class SoftDecimalMarketOrderRouter : SoftMarketOrderRouter
    {
        #region Overriden Methods

        protected override Order BuildOrder(Position pos, int index, double qty)
        {
            string clOrdId = Guid.NewGuid().ToString();
            Order order = new Order()
            {
                Security = pos.Security,
                ClOrdId = clOrdId,
                OrigClOrdId = clOrdId,
                Side = pos.Side,

                OrdType = OrdType.Limit,
                Price = pos.Side == Side.Buy ? pos.Security.MarketData.BestAskPrice : pos.Security.MarketData.BestBidPrice,

                DecimalPrecission = DecimalPrecissionConverter.GetDecimalPrecission(pos),

                TimeInForce = TimeInForce.Day,
                Currency = pos.Security.Currency,
                QuantityType = pos.QuantityType,
                PriceType = PriceType.FixedAmount,
                Account = pos.AccountId,
                Index = index,// this index is the order number in the fina order list
                OrderQty = qty
            };

            pos.LeavesQty = qty;

            return order;
        
        
        }

        #endregion
    }
}

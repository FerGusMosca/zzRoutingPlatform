using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.OrderRouters.Common.Util;

namespace zHFT.OrderRouters.Router
{
    public class DecimalOrderRouter:OrderRouter
    {
     
        #region Protected Methods

        protected override  Order BuildOrder(Position pos, Side side, int index)
        {
            Order order = new Order()
            {
                Security = pos.Security,
                ClOrdId = pos.GetNextClOrdId(index + (ORConfiguration.OrderIdStart.HasValue ? ORConfiguration.OrderIdStart.Value : 0)),
                Side = side,
                OrdType = OrdType.Limit,
                Price = side == Side.Buy ? pos.Security.MarketData.BestBidPrice : pos.Security.MarketData.BestAskPrice,
                CashOrderQty = side == Side.Buy ? Convert.ToDouble(pos.Security.MarketData.BestAskCashSize) : Convert.ToDouble(pos.Security.MarketData.BestBidCashSize),
                TimeInForce = TimeInForce.Day,
                Currency = pos.Security.Currency,
                QuantityType = pos.QuantityType,
                PriceType = PriceType.FixedAmount,
                DecimalPrecission=DecimalPrecissionConverter.GetDecimalPrecission(pos),
                Account = pos.AccountId,
                Index = index
            };

            order.OrigClOrdId = order.ClOrdId;

            if (pos.IsMonetaryQuantity())
            {
                double qty = pos.CashQty.Value / order.Price.Value;
                order.OrderQty = qty;
                pos.LeavesQty = qty;//Position Missing to fill in shares
            }
            else if (pos.IsNonMonetaryQuantity())
            {
                order.OrderQty = pos.Qty;
                pos.LeavesQty = pos.Qty;//Position Missing to fill in amount of shares
            }
            else
                throw new Exception("Could not process position quantity type: " + pos.QuantityType.ToString());

            return order;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.Router
{
    public class BackTestOrderRouter:MarketOrderRouter
    {
        protected override Order BuildOrder(Position pos, Side side, int index)
        {

            if (pos.TriggerPrice == null || !pos.TriggerPrice.Trade.HasValue)
                throw new Exception($"CANNOT route a position without a trigger price when backtesting");

            MarketData md = pos.TriggerPrice;

            Order order = new Order()
            {
                Security = pos.Security,
                ClOrdId = pos.GetNextClOrdId(index + (ORConfiguration.OrderIdStart.HasValue ? ORConfiguration.OrderIdStart.Value : 0)),
                Side = side,
                OrdType = OrdType.Limit,
                TimeInForce = TimeInForce.Day,
                Currency = Currency.USD.ToString(),
                QuantityType = pos.QuantityType,
                PriceType = PriceType.FixedAmount,
                Account = pos.AccountId,
                Price = md.Trade,
                Index = index
            };
            order.OrigClOrdId = order.ClOrdId;

            if (pos.IsMonetaryQuantity())
            {
                double qty = Math.Floor(pos.CashQty.Value / order.Price.Value);
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
    }
}

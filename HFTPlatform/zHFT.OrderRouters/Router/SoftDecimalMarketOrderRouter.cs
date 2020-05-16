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

        protected override double GetFullOrderQty(Position pos, double price)
        {
            //if (!pos.IsSinlgeUnitSecurity())
            //    throw new Exception(string.Format("Not implemented quantity conversion for security type {0}", pos.Security.SecType.ToString()));

            if (pos.CumQty == 0)
            {
                if (pos.IsMonetaryQuantity())
                {
                    pos.Qty = Convert.ToDouble(pos.CashQty.Value / price);
                    pos.CashQty = null;//this doesn't apply anymore
                    return Convert.ToDouble(pos.Qty);
                }
                else
                {
                    if (pos.Qty.HasValue)
                        return Convert.ToDouble(pos.Qty.Value);
                    else
                        throw new Exception(string.Format("Missing quantity for new order for security {0}", pos.Security.Symbol));
                }
            }
            else
            {
                //We had some traeds, now we use the LeavesQty
                return Convert.ToDouble(pos.LeavesQty);
            }
        }

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

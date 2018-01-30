using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.Router
{
    public class DecimalOrderRouter:OrderRouter
    {
        #region Protected Methods

        private int GetDecimalPlaces(decimal decimalNumber)
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

        private int GetDecimalPrecission(Position pos)
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

        #endregion

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
                DecimalPrecission=GetDecimalPrecission(pos),
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

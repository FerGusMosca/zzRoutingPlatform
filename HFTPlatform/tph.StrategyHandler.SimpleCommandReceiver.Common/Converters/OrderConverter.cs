using System;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Converters
{
    public class OrderConverter
    {
        #region Public Attributes

        public Order ConvertNewOrder(RouteOrderReq newOrdeReq)
        {
            Order order = new Order();

            order.OrderId = null;
            order.ClOrdId = newOrdeReq.ClOrdId;
            order.OrigClOrdId = null;
            order.OrderQty = newOrdeReq.Qty;
            order.CashOrderQty = null;
            order.OrdType = newOrdeReq.LimitPrice != null ? OrdType.Limit : OrdType.Market;
            order.Price = newOrdeReq.LimitPrice;
            order.StopPx = null;
            order.Currency = newOrdeReq.Currency;
            order.ExpireTime = null;
            order.MinQty = null;
            order.Side = newOrdeReq.GetSide();
            order.QuantityType = QuantityType.SHARES;
            order.PriceType = PriceType.FixedAmount;

            order.Security = new Security();
            order.Security.Symbol = newOrdeReq.Symbol;
            
            return order;
        }

        #endregion
    }
}
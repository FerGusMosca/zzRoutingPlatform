using System;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Converters
{
    public class OrderConverter
    {
        #region Public Static Methods
        
        public static Security GetSecurityFullSymbol(string fullSymbol)
        {
            string[] fields = fullSymbol.Split(new string[] {"."},StringSplitOptions.RemoveEmptyEntries);

            string symbol = fields[0];
            string exchange = fields.Length >= 2 ? fields[1] : null;
            string strSecType = fields.Length >= 3 ? fields[2] : null;

            exchange = exchange != "*" ? exchange : null;
            strSecType = strSecType != "*" ? strSecType : null;

            SecurityType secType = SecurityType.CS;//Default security type

            if (strSecType != null)
                secType = Security.GetSecurityType(strSecType);

            return new Security() {Symbol = symbol, Exchange = exchange, SecType = secType};

        }

        private void ValidateNewOrder(NewOrderReq newOrdeReq)
        {
            if (newOrdeReq.Type == NewOrderReq._ORD_TYPE_LIMIT && !newOrdeReq.Price.HasValue)
                throw new Exception(string.Format("New Order {0} is marked as Limit but does not have a price",
                    newOrdeReq.ClOrdId));
            
            if (newOrdeReq.Type == NewOrderReq._ORD_TYPE_MKT && newOrdeReq.Price.HasValue)
                throw new Exception(string.Format("New Order {0} is marked as Market but it has a price:{1}",
                    newOrdeReq.ClOrdId,newOrdeReq.Price.Value));

        }

        #endregion
        
        #region Public Attributes

        public Order ConvertNewOrder(NewOrderReq newOrdeReq)
        {
            ValidateNewOrder(newOrdeReq);
            
            Order order = new Order();

            order.OrderId = null;
            order.Account = newOrdeReq.Account;
            order.ClOrdId = newOrdeReq.ClOrdId;
            order.OrigClOrdId = null;
            order.OrderQty = newOrdeReq.Qty;
            order.CashOrderQty = null;
            order.OrdType = newOrdeReq.Price != null ? OrdType.Limit : OrdType.Market;
            order.Price = newOrdeReq.Price;
            order.StopPx = null;
            order.Currency = newOrdeReq.Currency;
            order.ExpireTime = null;
            order.MinQty = null;
            order.Side = newOrdeReq.GetSide();
            order.QuantityType = QuantityType.SHARES;
            order.PriceType = PriceType.FixedAmount;

            order.Security = GetSecurityFullSymbol(newOrdeReq.Symbol);
            order.Exchange = order.Security.Exchange;
            return order;
        }

        #endregion
    }
}
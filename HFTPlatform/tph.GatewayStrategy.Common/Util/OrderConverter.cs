using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace tph.GatewayStrategy.Common.Util
{
    public class OrderConverter : ConverterBase
    {
        #region Protected Static Methods

        protected void ValidateNewOrder(Wrapper wrapper)
        {
            if (!ValidateField(wrapper, OrderFields.Account))
                throw new Exception("Missing account for order");

            if (!ValidateField(wrapper, OrderFields.OrdType))
                throw new Exception("Missing order type for order");

            if (!ValidateField(wrapper, OrderFields.Side))
                throw new Exception("Missing side for order");

            if (!ValidateField(wrapper, OrderFields.Symbol))
                throw new Exception("Missing symbol for order");

            //if (!ValidateField(wrapper, OrderFields.Exchange))
            //    throw new Exception("Missing exchange for order");

            if (!ValidateField(wrapper, OrderFields.SecurityType))
                throw new Exception("Missing Security Type for order");

        }


        private QuantityType? GetQuantityType(double? ordQty, double? cashOrdQty)
        {
            QuantityType? qtyType = null;
            if (cashOrdQty.HasValue)
                qtyType = QuantityType.CURRENCY;
            else if (ordQty.HasValue)
                qtyType = QuantityType.SHARES;
            else
                throw new Exception($"Order must have weather a cash qty. or nominal qty.");

            return qtyType;
        }

        #endregion


        #region Public Static Methods
        public Order ConvertNewOrder(Wrapper wrapper)
        {
            ValidateNewOrder(wrapper);

            zHFT.Main.Common.Enums.OrdType ordType = (zHFT.Main.Common.Enums.OrdType)wrapper.GetField(OrderFields.OrdType);
            double? price = (double?)wrapper.GetField(OrderFields.Price);

            if (ordType == OrdType.Limit || ordType == OrdType.LimitOnClose)
            {
                if (price == null)
                    throw new Exception(string.Format("Missing price for limit order"));
            }

            double? stopPx = (double?)wrapper.GetField(OrderFields.StopPx);

            if (ordType == OrdType.StopLimit)
            {
                if (stopPx == null)
                    throw new Exception(string.Format("Missing stop price for stop limit order"));
            }

            zHFT.Main.Common.Enums.Side side = (zHFT.Main.Common.Enums.Side)wrapper.GetField(OrderFields.Side);
            zHFT.Main.Common.Enums.TimeInForce? tif = (zHFT.Main.Common.Enums.TimeInForce?)wrapper.GetField(OrderFields.TimeInForce);
            zHFT.Main.Common.Enums.SettlType? settlType = (zHFT.Main.Common.Enums.SettlType?)wrapper.GetField(OrderFields.SettlType);
            double? ordQty = (double?)wrapper.GetField(OrderFields.OrderQty);
            double? cashOrdQty = (double?)wrapper.GetField(OrderFields.CashOrderQty);
            string account = (string)wrapper.GetField(OrderFields.Account);
            string symbol = (string)wrapper.GetField(OrderFields.Symbol);
            SecurityType secType = (SecurityType)wrapper.GetField(OrderFields.SecurityType);
            string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);
            string origClOrdId = (string)wrapper.GetField(OrderFields.OrigClOrdID);
            string currency = (string)wrapper.GetField(OrderFields.Currency);
            string exchange = (string)wrapper.GetField(OrderFields.Exchange);
            QuantityType? qtyType = GetQuantityType(ordQty,cashOrdQty);
           

            Order order = new Order();
            order.ClOrdId = clOrdId;
            order.OrigClOrdId = origClOrdId;
            order.OrdType = ordType;
            order.Price = price;
            order.StopPx = stopPx;
            order.Side = side;
            order.TimeInForce = tif;
            order.SettlType = settlType;
            order.OrderQty = ordQty;
            order.CashOrderQty = cashOrdQty;
            order.Account = account;
            order.Symbol = symbol;
            order.Currency = currency;
            order.Exchange = exchange;
            order.QuantityType = qtyType.Value;
            order.Security = new Security() { Symbol = symbol, SecType = secType };

            return order;
        }

        #endregion
    }
}

using System;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Converter;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common.Converters;

namespace tph.OrderRouter.Cocos.Common.Converters
{
    public class OrderConverter:ConverterBase
    {
        
        #region Protected Static Methods

        public new static void ValidateNewOrder(Wrapper wrapper)
        {
            
        }

        #endregion
        
        #region Public Static Methods

        public  static Order GetNewOrder(Wrapper wrapper)
        {
            Order order = new Order();

            ValidateNewOrder(wrapper);

            string fullSymbol = (string)wrapper.GetField(OrderFields.Symbol);

            order.Symbol = SymbolConverter.GetCleanSymbolFromFullSymbol(fullSymbol);
           
            order.Exchange = ExchangeConverter.GetExchange(fullSymbol);

            order.OrderQty = Convert.ToInt64(wrapper.GetField(OrderFields.OrderQty)); 
            order.Price = (double?)wrapper.GetField(OrderFields.Price);
            order.Side = (Side)wrapper.GetField(OrderFields.Side);
            order.OrdType = (OrdType)wrapper.GetField(OrderFields.OrdType);
            //order.modalidad = _ORD_TYPE_MARKET;
            
            order.ClOrdId = wrapper.GetField(OrderFields.ClOrdID).ToString();

            TimeInForce tif = (TimeInForce)wrapper.GetField(OrderFields.TimeInForce);

            SettlType? settlType = (zHFT.Main.Common.Enums.SettlType?)wrapper.GetField(OrderFields.SettlType);

            return order;
        }


        #endregion
    }
}
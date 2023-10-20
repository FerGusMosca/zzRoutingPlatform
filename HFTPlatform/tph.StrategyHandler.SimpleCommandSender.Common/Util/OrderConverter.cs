using System;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandSender.Common.Util
{
    public class OrderConverter
    {
        #region Protected Methods

        private void ValidateNewOrder(Order newOrder)
        {
            if (newOrder.OrdType == OrdType.Limit && !newOrder.Price.HasValue)
                throw new Exception(string.Format("New Order {0} is marked as Limit but does not have a price",
                    newOrder.ClOrdId));
            
            if (newOrder.OrdType == OrdType.Market && newOrder.Price.HasValue)
                throw new Exception(string.Format("New Order {0} is marked as Market but it has a price:{1}",
                    newOrder.ClOrdId,newOrder.Price.Value));

            if(string.IsNullOrEmpty(newOrder.ClOrdId))
                throw new Exception($"Could not find ClOrdId for new Order for symbol {newOrder.Security.Symbol}");
            
            if(string.IsNullOrEmpty(newOrder.Security.Symbol))
                throw new Exception($"Could not find Symbol for new Order for ClOrdId {newOrder.ClOrdId}");
            
            if(string.IsNullOrEmpty(newOrder.Side.ToString()))
                throw new Exception($"Could not find Symbol for new Order for symbol {newOrder.Security.Symbol}");
            
        }


        #endregion
        
        
        #region Public Methods

        public NewOrderReq  ConvertNewOrder(Order newOrder)
        {
            ValidateNewOrder(newOrder);
            
            NewOrderReq newOrderReqJson = new NewOrderReq();

            newOrderReqJson.UUID = Guid.NewGuid().ToString();
            newOrderReqJson.ReqId = Guid.NewGuid().ToString();
            newOrderReqJson.Account = newOrder.Account;
            newOrderReqJson.ClOrdId = newOrder.ClOrdId;

            newOrderReqJson.Qty = newOrder.OrderQty.HasValue ? newOrder.OrderQty.Value : 0;
            newOrderReqJson.Type = newOrder.Price != null ? NewOrderReq._ORD_TYPE_LIMIT : NewOrderReq._ORD_TYPE_MKT;
            newOrderReqJson.Price = newOrder.Price;
            newOrderReqJson.Currency = newOrder.Currency;

            if (newOrder.Side == Side.Buy)
                newOrderReqJson.Side = NewOrderReq._BUY;
            else if (newOrder.Side == Side.Sell)
                newOrderReqJson.Side = NewOrderReq._SELL;
            else
                throw new Exception($"Side not recognized for Websocket connector client: {newOrder.Side}");
            
            if (newOrder.TimeInForce == TimeInForce.Day)
                newOrderReqJson.TimeInForce =NewOrderReq._TIF_DAY;
            else if (newOrder.TimeInForce == TimeInForce.GoodTillCancel)
                newOrderReqJson.TimeInForce =NewOrderReq._TIF_GTC;
            else
                newOrderReqJson.TimeInForce =NewOrderReq._TIF_UNK;

            newOrderReqJson.Symbol = newOrder.Security.Symbol;


            return newOrderReqJson;
        }

        #endregion

    }
}
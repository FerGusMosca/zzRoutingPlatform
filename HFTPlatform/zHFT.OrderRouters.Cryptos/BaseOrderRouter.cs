using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common;

namespace zHFT.OrderRouters.Cryptos
{
    public abstract class BaseOrderRouter : OrderRouterBase, ICommunicationModule
    {
        #region Protected Attributes

        protected Dictionary<string, Order> ActiveOrders { get; set; }

        protected Dictionary<string, string> OrderIdMappers { get; set; }

        protected Thread ExecutionReportThread { get; set; }

        protected List<string> CanceledOrders { get; set; }

        protected object tLock { get; set; }

        #endregion

        #region Public Abstract Methods

        protected abstract BaseConfiguration GetConfig();

        protected abstract bool RunCancelOrder(Order order, bool update);

        protected abstract string GetQuoteCurrency();

        protected abstract CMState RouteNewOrder(Wrapper wrapper);
        protected abstract CMState UpdateOrder(Wrapper wrapper);
        protected abstract CMState CancelOrder(Wrapper wrapper);

        protected abstract CMState ProcessSecurityList(Wrapper wrapper);

        public abstract bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile);

        #endregion

        #region Protected Methods

        protected override CMState ProcessIncoming(Wrapper wrapper)
        {

            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No incoming module set for market order router!"));
        }

        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No outgoing module set for market order router!"));
        }

        protected Order GetOrder(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(OrderFields.Symbol);
            double? price = (double?)wrapper.GetField(OrderFields.Price);
            Side side = (Side)wrapper.GetField(OrderFields.Side);
            double orderQty = (double)wrapper.GetField(OrderFields.OrderQty);
            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            int decimalPrecission = (int)wrapper.GetField(OrderFields.DecimalPrecission);

            if (!price.HasValue)
                throw new Exception(string.Format("Las ordenes deben tener un precio asignado. No se puede rutear orden para moneda {0}", symbol));


            Order order = new Order()
            {
                Symbol = symbol,
                Price = price,
                Side = side,
                OrderQty = orderQty,
                Currency = GetQuoteCurrency(),
                OrdType = OrdType.Limit,
                ClOrdId = clOrderId,
                DecimalPrecission=decimalPrecission
            };

            return order;
        }

        protected CMState CancelAllActiveOrders()
        {
            try
            {
                lock (tLock)
                {
                    foreach (string uuid in ActiveOrders.Keys)
                    {
                        Order order = ActiveOrders[uuid];
                        DoLog(string.Format("@{0}:Cancelling active order for symbol {1}", GetConfig().Name, order.Security.Symbol), Main.Common.Util.Constants.MessageType.Information);

                        RunCancelOrder(order, false);
                    }

                    return CMState.BuildSuccess();
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelling all active orders!:{1}", GetConfig().Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }

        }

        #endregion

        #region Public Methods

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("@{1}:Routing  to market for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString(), GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);
                    return RouteNewOrder(wrapper);
                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("@{1}:Updating order  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString(), GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);
                    return UpdateOrder(wrapper);
                    //return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("@{1}:Cancelling order  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString(), GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);
                    return CancelOrder(wrapper);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders", GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);
                    return CancelAllActiveOrders();
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    DoLog(string.Format("@{0}:Receiving Security List", GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);
                    return ProcessSecurityList(wrapper);
                }
                else
                {
                    DoLog(string.Format("@{1}:Could not process order routing for action {0} :", wrapper.GetAction().ToString(), GetConfig().Name),
                          Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("@{1}:Could not process order routing for action {0}:", wrapper.GetAction().ToString(), GetConfig().Name)));
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error routing order to market:" + ex.Message, GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }

        }

        #endregion

    }
}

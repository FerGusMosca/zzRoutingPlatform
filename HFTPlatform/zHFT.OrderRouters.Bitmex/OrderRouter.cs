using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bitmex.BusinessEntities;
using zHFT.OrderRouters.Bitmex.DataAccessLayer;
using zHFT.OrderRouters.Cryptos;

namespace zHFT.OrderRouters.Bitmex
{
    public class OrderRouter : BaseOrderRouter
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration BitmexConfiguration { get; set; }

        protected OrderManager OrderManager { get; set; }

        protected Dictionary<string, Order> BitMexActiveOrders { get; set; }

        #endregion


        #region Overriden Methods

        protected override BaseConfiguration GetConfig()
        {
            return BitmexConfiguration;
        }

        protected override string GetQuoteCurrency()
        {
            return null;
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            BitmexConfiguration = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected Order GetOrder(Wrapper wrapper)
        {
            string symbol = (string)wrapper.GetField(OrderFields.Symbol);
            decimal? price = (decimal?)wrapper.GetField(OrderFields.Price);
            Side side = (Side)wrapper.GetField(OrderFields.Side);
            decimal orderQty = (decimal)wrapper.GetField(OrderFields.OrderQty);
            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            int decimalPrecission = (int)wrapper.GetField(OrderFields.DecimalPrecission);

            if (!price.HasValue)
                throw new Exception(string.Format("Las ordenes deben tener un precio asignado. No se puede rutear orden para moneda {0}", symbol));


            Order order = new Order()
            {
                SymbolPair=symbol,
                Price = price,
                Side = side,
                OrderQty = orderQty,
                //Currency = GetQuoteCurrency(),
                OrdType = OrdType.Limit,
                ClOrdId = clOrderId,
            };

            return order;
        }

        protected override CMState RouteNewOrder(Wrapper wrapper)
        {
            try
            {
                Order order = GetOrder(wrapper);
                try
                {
                    lock (tLock)
                    {
                        ExecutionReport exRep = OrderManager.PlaceOrder(order);
                        //TODO: Publicar el execution report
                    }
                }
                catch (Exception ex)
                {
                    //TODO: Rejected Execution Report
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }

        }

        protected override CMState UpdateOrder(Wrapper wrapper)
        {

            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
            string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
            try
            {

                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (tLock)
                {

                    if (OrderIdMappers.ContainsKey(origClOrderId))
                    {
                        string origUuid = OrderIdMappers[origClOrderId];
                        Order order = BitMexActiveOrders[origUuid];

                        if (order != null)
                        {
                            ////Recuperamos la orden
                            //GetOrderResponse ordResp = RunGetOrder(order.OrderId);

                            ////Cancelamos
                            //RunCancelOrder(order, true);

                            //Thread.Sleep(100);

                            ////Damos el alta
                            //double? newPrice = (double?)wrapper.GetField(OrderFields.Price);
                            //order.Price = newPrice;
                            //order.ClOrdId = clOrderId;
                            //order.OrderQty = ordResp != null ? Convert.ToDouble(ordResp.QuantityRemaining) : order.OrderQty;//Nos aseguramos de solo rutear la nueva cantidad
                            //try
                            //{
                            //    RunNewOrder(order, true);
                            //}
                            //catch (Exception ex)
                            //{
                            //    if (ActiveOrders.ContainsKey(order.OrderId))
                            //        ActiveOrders.Remove(order.OrderId);
                            //    EvalRouteError(order, ex);
                            //}
                        }

                    }
                    else
                        DoLog(string.Format("@{0}:Could not find order for origClOrderId  {1}!", BitmexConfiguration.Name, origClOrderId), Main.Common.Util.Constants.MessageType.Error);

                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error updating order {1}!:{2}", BitmexConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected override bool RunCancelOrder(zHFT.Main.BusinessEntities.Orders.Order order, bool update)
        {
            throw new NotImplementedException("Not implemented");
        }

        protected bool RunCancelOrder(Order order, bool update)
        {
            //TODO implementar cancelación --> Ver si vale la pena implementar el CancellAllOrders
            return true;
        }

        protected override CMState CancelOrder(Wrapper wrapper)
        {
            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
            try
            {
                //New order id
                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

                lock (tLock)
                {

                    if (OrderIdMappers.ContainsKey(origClOrderId))
                    {
                        string uuid = OrderIdMappers[origClOrderId];
                        Order order = BitMexActiveOrders[uuid];

                        if (order != null)
                        {
                            RunCancelOrder(order, false);
                        }
                        OrderIdMappers.Remove(origClOrderId);
                    }

                    else
                    {
                        throw new Exception(string.Format("Could not cancel order for id {0}", origClOrderId));
                        //TO DO: La orden fue modificada
                        //buscar con el nuevo clOrderId
                    }
                }
                return CMState.BuildSuccess();

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelig order {1}!:{2}", BitmexConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }


        #endregion

        #region Public Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLock = new object();

                    BitMexActiveOrders = new Dictionary<string,Order>();
                    OrderManager = new DataAccessLayer.OrderManager(BitmexConfiguration.URL, BitmexConfiguration.ApiKey, BitmexConfiguration.Secret);

                    CanceledOrders = new List<string>();
                    
                    //ExecutionReportThread = new Thread(DoEvalExecutionReport);
                    //ExecutionReportThread.Start();
                    return true;
                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing " + configFile + ":" + ex.Message, BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}

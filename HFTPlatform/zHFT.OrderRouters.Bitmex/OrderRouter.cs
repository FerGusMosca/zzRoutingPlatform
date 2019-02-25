using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bitmex.BusinessEntities;
using zHFT.OrderRouters.Bitmex.Common.DTO.Events;
using zHFT.OrderRouters.Bitmex.Common.Wrappers;
using zHFT.OrderRouters.Bitmex.DataAccessLayer;
using zHFT.OrderRouters.Cryptos;
using zHFT.StrategyHandler.Common.Converters;

namespace zHFT.OrderRouters.Bitmex
{
    public class OrderRouter : BaseOrderRouter
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration BitmexConfiguration { get; set; }

        protected OrderManager OrderManager { get; set; }

        protected  zHFT.OrderRouters.Bitmex.DataAccessLayer.Websockets.OrderManager WSOrderManager { get; set; }

        protected Dictionary<string, Order> BitMexActiveOrders { get; set; }

        protected List<Security> Securities { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }

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

        #endregion

      
        #region Protected Methods

        protected void HandleGenericSubscription(WebSocketResponseMessage WebSocketResponseMessage)
        {
            WebSocketSubscriptionResponse resp = (WebSocketSubscriptionResponse)WebSocketResponseMessage;

            if (resp.success)
                DoLog(string.Format("Successfully subscribed to {0} event ",
                                            resp.GetSubscriptionEvent()), Main.Common.Util.Constants.MessageType.Information);
            else
                Console.WriteLine(string.Format("Error on subscription to {0} event:{!}",
                                            resp.GetSubscriptionEvent(), resp.error), Main.Common.Util.Constants.MessageType.Error);
        }

        protected void ExecutionReportSubscriptionResponse(WebSocketResponseMessage WebSocketResponseMessage)
        {
            HandleGenericSubscription(WebSocketResponseMessage);
        }

        protected void ProcessExecutionReports(WebSocketSubscriptionEvent subscrEvent)
        {
            WebSocketExecutionReportEvent reports = (WebSocketExecutionReportEvent)subscrEvent;
            foreach (zHFT.OrderRouters.Bitmex.Common.DTO.ExecutionReport execReportDTO in reports.data)
            {
                try
                {
                    //TODO: Build Execution Report and publish
                    //Console.WriteLine(string.Format(@"============Showing Execution Report for action {6}=> Side:{9} orderId:{0} " + Environment.NewLine +
                    //                                @" pair {1} OrdQty={10} OrdPrice={11}  ExecType={2} OrdStatus={3} CumQty={4} LeavesQty={5} LastPx={7} LastShares={8}" + Environment.NewLine +
                    //                                @" AvgPx={12}",
                    //                                execReportDTO.OrderID, execReportDTO.Symbol, execReportDTO.ExecType,
                    //                                execReportDTO.OrdStatus, execReportDTO.CumQty, execReportDTO.LeavesQty, reports.action,
                    //                                execReportDTO.LastPx.HasValue ? execReportDTO.LastPx.Value.ToString() : "",
                    //                                execReportDTO.LastQty.HasValue ? execReportDTO.LastQty.Value.ToString() : "",
                    //                                execReportDTO.Side,
                    //                                execReportDTO.OrderQty.HasValue ? execReportDTO.OrderQty.Value.ToString() : "",
                    //                                execReportDTO.Price.HasValue ? execReportDTO.Price.Value.ToString() : "",
                    //                                execReportDTO.AvgPx.HasValue ? execReportDTO.AvgPx.Value.ToString() : ""
                    //                                ));

                    //Console.WriteLine("");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error on subscription to {0} :{1}", subscrEvent.GetSubscriptionEvent(), ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }
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
                SymbolPair=symbol,
                Price = Convert.ToDecimal(price),
                Side = side,
                OrderQty = Convert.ToDecimal(orderQty),
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

        protected override CMState ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                SecurityList secList = SecurityListConverter.GetSecurityList(wrapper, Config);
                Securities = secList.Securities;

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
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
                    OrderManager = new DataAccessLayer.OrderManager(BitmexConfiguration.RESTURL, BitmexConfiguration.ApiKey, BitmexConfiguration.Secret);
                    WSOrderManager = new DataAccessLayer.Websockets.OrderManager(BitmexConfiguration.WebsocketURL, new UserCredentials() { BitMexID = BitmexConfiguration.ApiKey, BitMexSecret = BitmexConfiguration.Secret });
                    SecurityListConverter = new StrategyHandler.Common.Converters.SecurityListConverter();

                    WSOrderManager.SubscribeResponseRequest(
                                                         DataAccessLayer.Websockets.OrderManager._EXECUTIONS,
                                                         ExecutionReportSubscriptionResponse,
                                                         new object[] { });

                    WSOrderManager.SubscribeEvents(DataAccessLayer.Websockets.OrderManager._EXECUTIONS, ProcessExecutionReports);

                    WSOrderManager.SubscribeExecutions();

                    SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities, null);
                    OnMessageRcv(slWrapper);
                    
                    CanceledOrders = new List<string>();
                    
                    
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

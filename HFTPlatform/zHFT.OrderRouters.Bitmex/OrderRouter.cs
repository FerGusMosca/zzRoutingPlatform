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
                DoLog(string.Format("Error on subscription to {0} event:{!}",
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

                    string clOrdId = "";
                    if (OrderIdMappers.ContainsKey(execReportDTO.OrderID))
                    {
                        clOrdId = OrderIdMappers[execReportDTO.OrderID];

                        if (BitMexActiveOrders.ContainsKey(clOrdId))
                        {
                            Order order = BitMexActiveOrders[clOrdId];

                            lock (tLock)
                            {

                                if (execReportDTO.ExecType == ExecType.New.ToString())
                                {
                                    order.OrderId = execReportDTO.OrderID;
                                }

                                if (execReportDTO.ExecType == ExecType.Replaced.ToString())
                                {
                                    BitMexActiveOrders.Remove(clOrdId);
                                    order.ClOrdId = order.PendingClOrdId;
                                    order.PendingClOrdId = null;
                                    BitMexActiveOrders.Add(order.ClOrdId, order);
                                    OrderIdMappers[execReportDTO.OrderID] = order.ClOrdId;
                                }

                                if (execReportDTO.ExecType == ExecType.Canceled.ToString() ||
                                    execReportDTO.ExecType == ExecType.Stopped.ToString() ||
                                    execReportDTO.ExecType == ExecType.Rejected.ToString() ||
                                    execReportDTO.ExecType == ExecType.Suspended.ToString() ||
                                    execReportDTO.ExecType == ExecType.Expired.ToString())
                                {

                                    BitMexActiveOrders.Remove(order.ClOrdId);
                                    OrderIdMappers.Remove(order.OrderId);
                                }
                            }

                            WSExecutionReportWrapper wrapper = new WSExecutionReportWrapper(execReportDTO, order);
                            OnMessageRcv(wrapper);
                        }
                    }
                    else
                        DoLog(string.Format("Unknown order processing execution report for OrderId {0} ", execReportDTO.OrderID), Main.Common.Util.Constants.MessageType.Information);

                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Error processing execution report for ClOrdId {0}:{1} ", execReportDTO.ClOrdID, ex.Message), Main.Common.Util.Constants.MessageType.Error);
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

            //orderQty tiene el valor monetario en USD de la posición
            //tenemos que obtener la cantidad de contratos a comprar

            if (Securities.Any(x => x.Symbol == symbol))
            {
                Security sec = Securities.Where(x => x.Symbol == symbol).FirstOrDefault();
                double contracts = 0;
                if (sec.MarginRatio.HasValue && sec.ContractSize.HasValue)
                    contracts = Math.Floor(orderQty / (sec.MarginRatio.Value * Convert.ToDouble(sec.ContractSize.Value)));
                else
                    throw new Exception(string.Format("No margin ratio or contract size found for symbol {0}. Those values are needed @BitMex to calculate order Qty", symbol));

                if (!price.HasValue)
                    throw new Exception(string.Format("Las ordenes deben tener un precio asignado. No se puede rutear orden para moneda {0}", symbol));


                Order order = new Order()
                {
                    SymbolPair = symbol,
                    Price = Convert.ToDecimal(price),
                    Side = side,
                    OrderQty = Convert.ToDecimal(contracts),
                    //Currency = GetQuoteCurrency(),
                    OrdType = OrdType.Limit,
                    ClOrdId = clOrderId,
                };

                return order;
            }
            else
                throw new Exception(string.Format("No securities found in memory for symbol {0}", symbol));
        }

        protected ExecutionReport GetRejectedExecutionReport(Order order,string error)
        {

            ExecutionReport rejectedER = new ExecutionReport();
            rejectedER.OrderID = order.OrderId;
            rejectedER.ClOrdID = order.ClOrdId;
            rejectedER.Symbol = order.SymbolPair;
            rejectedER.Side = order.Side.ToString();
            rejectedER.OrderQty = order.OrderQty;
            rejectedER.Price = order.Price.HasValue ? (double?)Convert.ToDouble(order.Price.Value) : null ;
            rejectedER.StopPx = order.StopPx.HasValue ? (double?)Convert.ToDouble(order.StopPx.Value) : null;
            rejectedER.Currency = order.Currency;
            rejectedER.OrdType = order.OrdType.ToString();
            rejectedER.ExecType = ExecType.Rejected.ToString();
            rejectedER.OrdStatus = OrdStatus.Rejected.ToString();
            rejectedER.OrdRejReason = OrdRejReason.Other.ToString();
            rejectedER.Text = error;
            rejectedER.Timestamp = DateTime.Now;
            rejectedER.TransactTime = DateTime.Now;
            rejectedER.Order = order;

            return rejectedER;
        }

        private void RunUpperTick(Order order)
        {
            if (order.Side == Side.Buy)
                order.Price += 0.5m;
            else
                order.Price -= 0.5m;
        
        
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
                        //RunUpperTick(order);
                        ExecutionReport exRep = OrderManager.PlaceOrder(order);
                        BitMexActiveOrders.Add(order.ClOrdId, order);
                        if (exRep.OrdStatus == OrdStatus.New.ToString())
                            OrderIdMappers.Add(exRep.OrderID, order.ClOrdId);
                        ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(exRep, order);
                        OnMessageRcv(erWrapper);
                    }
                }
                catch (Exception ex)
                {

                    ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(GetRejectedExecutionReport(order, ex.Message), order);
                    OnMessageRcv(erWrapper);
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
            double? price = (double?)wrapper.GetField(OrderFields.Price);
            try
            {

                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (tLock)
                {

                    if (BitMexActiveOrders.ContainsKey(origClOrderId) && price.HasValue)
                    {
                        Order order = BitMexActiveOrders[origClOrderId];
                        order.Price = Convert.ToDecimal(price.Value);
                        order.PendingClOrdId = clOrderId;
                        ExecutionReport exRep = OrderManager.UpdateOrder(order);
                        

                        //ExecutionReportWrapper exWrapper = new ExecutionReportWrapper(exRep, order);
                        //OnMessageRcv(exWrapper);
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
            try
            {
                ExecutionReport exReport = null;
                lock (tLock)
                {
                    exReport = OrderManager.CancelOrder(order);
                   
                }

                //ExecutionReportWrapper wrapper = new ExecutionReportWrapper(exReport, order);
                //OnMessageRcv(wrapper);

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
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

                    if (BitMexActiveOrders.ContainsKey(origClOrderId))
                    {
                        Order order = BitMexActiveOrders[origClOrderId];
                        RunCancelOrder(order, false);
                    }
                    else
                    {
                        throw new Exception(string.Format("@{0}: Could not cancel order for OrigClOrdId {1} because it was not found", BitmexConfiguration.Name, origClOrderId));
                    }
                }
                return CMState.BuildSuccess();

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelling order {1}!:{2}", BitmexConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
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
                    OrderIdMappers = new Dictionary<string, string>();
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common;
using zHFT.OrderRouters.InvertirOnline.Common.Converters;
using zHFT.OrderRouters.InvertirOnline.Common.DTO;
using zHFT.OrderRouters.InvertirOnline.Common.Responses;
using zHFT.OrderRouters.InvertirOnline.Common.Wrappers;
using zHFT.OrderRouters.InvertirOnline.DataAccessLayer;

namespace zHFT.OrderRouters.InvertirOnline
{
    public class OrderRouter : zHFT.OrderRouters.InvertirOnline.Common.OrderRouterBase
    {
        #region Protected Attributes

        protected Common.Configuration.Configuration IOLConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected OrderConverter OrderConverter { get; set; }

        protected Dictionary<string, Order> ActiveOrders { get; set; }

        protected Dictionary<string, string> OrderIdsMapper { get; set; }

        protected Dictionary<string, Order> PendingUpdate { get; set; }

        protected IOLOrderRouterManager IOLOrderRouterManager { get; set; }

        protected Dictionary<string, ExecutionReportResp> LatestExecReport { get; set; }

        public static object tLock { get; set; }

        #endregion

        #region Potected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected void ProcessExecutionReport(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper)param;
                OnMessageRcv(wrapper);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error publishing execution report for new order: {0}",ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }
        
        }

        protected void EvalExecutionReportsThread(object param)
        {

            try
            {
                while (true)
                {
                    lock (tLock)
                    {
                        List<string> ordersToDel = new List<string>();
                        foreach (Order order in ActiveOrders.Values)
                        {
                            try
                            {
                                ExecutionReportResp execReport = IOLOrderRouterManager.GetExecutionReport(order.OrderId);

                                if (!PendingUpdate.ContainsKey(order.OrderId.ToString()))
                                {
                                    ExecutionReportWrapper execReportWrapper = new ExecutionReportWrapper(order, execReport);
                                    DoLog(string.Format(
                                            "@IOL Order Router - Publishing ER for symbol {0}. Status={1} ExecType={2} CumQty={3} LvsQty={4}",
                                            execReportWrapper.GetField(ExecutionReportFields.Symbol),
                                            execReportWrapper.GetField(ExecutionReportFields.OrdStatus),
                                            execReportWrapper.GetField(ExecutionReportFields.ExecType),
                                            execReportWrapper.GetField(ExecutionReportFields.CumQty),
                                            execReportWrapper.GetField(ExecutionReportFields.LeavesQty)),
                                        Constants.MessageType.Information);
                                    
                                    
                                    
                                    new Thread(ProcessExecutionReport).Start(execReportWrapper);

                                    if (!execReport.IsOpenOrder())
                                        ordersToDel.Add(order.OrderId.ToString());

                                    if (!LatestExecReport.ContainsKey(order.ClOrdId))
                                        LatestExecReport.Add(order.ClOrdId, execReport);
                                    else
                                        LatestExecReport[order.ClOrdId] = execReport;
                                }//TODO eval Pendingreplace ER on ELSE
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("Critical error evaluating execution report for orderId {0}!: {1}",order.OrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                            }
                        }

                        ordersToDel.ForEach(x => ActiveOrders.Remove(x));
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error evaluating execution reports!: {0}", ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }
        
        }

        protected NewOrderResponse DoRoute(Order order)
        {
            NewOrderResponse resp = null;

            if (order.side == Side.Buy)
                resp = IOLOrderRouterManager.Buy(order);
            else if (order.side == Side.Sell)
                resp = IOLOrderRouterManager.Sell(order);
            else
                throw new Exception(string.Format("Side not supported for the new order:{0}", order.side));

            if (resp.numeroOperacion.HasValue && resp.numeroOperacion.Value != 0)
                order.OrderId = resp.numeroOperacion.Value;
            else
                throw new Exception(string.Format("Market returned OrderId 0 or null for ClOrdId = {0}. Check with administrator.", order.ClOrdId));

            DoLog(string.Format("New order id {0} created for cl.order Id {1}", resp.numeroOperacion, order.ClOrdId),Main.Common.Util.Constants.MessageType.Information);
            return resp;
        }

        protected void RouteNewOrder(Wrapper wrapper)
        {
            Order order = null;
            NewOrderResponse resp = null;
            try
            {
                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (tLock)
                {
                    order = OrderConverter.GetNewOrder(wrapper, NextOrderId);
                    NextOrderId++;
                }

                DoLog(string.Format("Routing Client Order Id {0}", order.ClOrdId), Main.Common.Util.Constants.MessageType.Information);

                DoRoute(order);

                lock (tLock)
                {
                    OrderIdsMapper.Add(order.ClOrdId, order.OrderId.ToString());
                    ActiveOrders.Add(order.OrderId.ToString(), order);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error routing order {0} to the exchange!: {1}",order.ClOrdId, ex.Message), Main.Common.Util.Constants.MessageType.Error);

                RejectedExecutionReportWrapper rejectedWrapper = new RejectedExecutionReportWrapper(ex.Message, order);

                new Thread(ProcessExecutionReport).Start(rejectedWrapper);
            }
        }

        protected void CancelAllOrders()
        {
            lock (tLock)
            {

                foreach (string orderId in ActiveOrders.Keys)
                {

                    Order order = ActiveOrders[orderId];

                    DoCancel(order.ClOrdId, cancel: true);
                }
            }
        
        }

        protected ExecutionReportResp RunUpdateCancellation(Order activeOrder, Wrapper rejWrapper)
        {

            lock (tLock)
            {
                if (!PendingUpdate.ContainsKey(activeOrder.OrderId.ToString()))
                    PendingUpdate.Add(activeOrder.OrderId.ToString(),activeOrder);

                rejWrapper = DoCancel(activeOrder.ClOrdId, cancel: false);
            }

            bool cancelled = false;
            ExecutionReportResp execReport = null;
            DateTime cancelStarted = DateTime.Now;
            int i = 0;
            while (!cancelled)
            {

                execReport = IOLOrderRouterManager.GetExecutionReport(activeOrder.OrderId);
                cancelled = execReport.IsCancelled();
                Thread.Sleep(1000);
                i++;
                if (i > IOLConfiguration.CancellationTimeoutInSeconds)
                {
                    string msg=string.Format("Critical error!!!. OrderId {0} could not be cancelled and stays in status {1}", activeOrder.OrderId, execReport.estadoActual);
                    DoLog(msg,Main.Common.Util.Constants.MessageType.Error);

                    rejWrapper = new OrderCancelRejectWrapper(activeOrder.ClOrdId, activeOrder.OrderId.ToString(), CxlRejResponseTo.OrderCancelReplaceRequest,
                                                                CxlRejReason.Other, msg);

                    break;
                }

            }

            TimeSpan cancelElapsed = DateTime.Now - cancelStarted;

            DoLog(string.Format("Cancellation for OrderId {0} took {1} seconds", activeOrder.OrderId, cancelElapsed.TotalSeconds),
                            Main.Common.Util.Constants.MessageType.Information);

            return execReport;
        
        }

        protected void RunUpdateOrderCreation(Order activeOrder, Wrapper updateWrapper, Wrapper rejWrapper)
        {
            string newClOrdId = (string)updateWrapper.GetField(OrderFields.ClOrdID);
            string origClOrdId = (string)updateWrapper.GetField(OrderFields.OrigClOrdID);

            if (LatestExecReport.ContainsKey(activeOrder.ClOrdId))
            {
                ExecutionReportResp latestExecReport = LatestExecReport[activeOrder.ClOrdId];
                DoLog(string.Format("Order with ClOrdId {0} successfully cancelled", activeOrder.ClOrdId), Main.Common.Util.Constants.MessageType.Information);

                double? newPrice = (double?)updateWrapper.GetField(OrderFields.Price);
                double newQuantity = activeOrder.cantidad - latestExecReport.GetCumQty();//Nos aseguramos de solo rutear la nueva cantidad

                activeOrder.cantidad = newQuantity;
                activeOrder.precio = newPrice;
                activeOrder.ClOrdId = newClOrdId;

                lock (tLock)
                {
                    ActiveOrders.Remove(activeOrder.OrderId.ToString());

                    DoLog(string.Format("Sending new order with new price={0} and new qty={1} at {2}", newPrice, newQuantity,DateTime.Now), Main.Common.Util.Constants.MessageType.Information);

                    NewOrderResponse resp = DoRoute(activeOrder);

                    DoLog(string.Format("Order with new ClOrdId {0} successfully routed at {1}", newClOrdId, DateTime.Now), Main.Common.Util.Constants.MessageType.Information);


                    activeOrder.OrderId = resp.numeroOperacion.Value;

                    OrderIdsMapper.Add(newClOrdId, activeOrder.OrderId.ToString());

                    ActiveOrders.Add(activeOrder.OrderId.ToString(), activeOrder);
                }
            }
            else
                rejWrapper = new OrderCancelRejectWrapper(origClOrdId, "", CxlRejResponseTo.OrderCancelReplaceRequest, CxlRejReason.UnknownOrder,
                                                    string.Format("There is not an execution report for previously existing order {0}", origClOrdId));
        }

        protected void UpdateOrder(Wrapper wrapper, bool cancel)
        {
            string origClOrdId = (string)wrapper.GetField(OrderFields.OrigClOrdID);
            string newClOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);

            DoLog(string.Format("First we have to cancel ClOrdId {0} ", origClOrdId), Main.Common.Util.Constants.MessageType.Information);

            Wrapper rejWrapper = null;

                
            Order activeOrder = DoGetActiveOrder(origClOrdId);
                
            if (activeOrder != null)
            {
                try
                {
                    ExecutionReportResp execReport = RunUpdateCancellation(activeOrder, rejWrapper);

                    if (rejWrapper == null && execReport.IsCancelled())//cancellation is ok
                        RunUpdateOrderCreation(activeOrder, wrapper, rejWrapper);
                    else
                        if (rejWrapper == null)//La orden NO pudo se cancelada
                            rejWrapper = new OrderCancelRejectWrapper(origClOrdId, "", CxlRejResponseTo.OrderCancelReplaceRequest, CxlRejReason.UnknownOrder,
                                                                string.Format("The order {0} could not be cancelled in a prudent time. Aborting update", origClOrdId));
                }
                catch (Exception ex)
                {
                    throw;
                }
                finally
                {
                    if (PendingUpdate.ContainsKey(activeOrder.OrderId.ToString()))
                        PendingUpdate.Remove(activeOrder.OrderId.ToString());
                }
            }
            else
            {
                string msg = string.Format("Critical ERROR for OrigClOrdId={0}: Could not find an orderId after cancellation",origClOrdId);
                rejWrapper = new OrderCancelRejectWrapper(origClOrdId, "", CxlRejResponseTo.OrderCancelReplaceRequest,CxlRejReason.UnknownOrder,msg);
                DoLog(msg, Constants.MessageType.Information);
            }
            

            if (rejWrapper != null)
                OnMessageRcv(rejWrapper);
        }

        protected void UpdateOrderThread(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper)param;

                UpdateOrder(wrapper, cancel: false);
            }
            catch (Exception ex)
            {
                Wrapper rejWrapper = new OrderCancelRejectWrapper("", "", CxlRejResponseTo.OrderCancelReplaceRequest, CxlRejReason.UnknownOrder,
                        string.Format("CRITICAL ERROR @UpdateOrderThread:{0}", ex.Message));
            }
        
        }

        protected void CancelOrderThread(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper)param;

                CancelOrder(wrapper, cancel: true);
            }
            catch (Exception ex)
            {
                Wrapper rejWrapper = new OrderCancelRejectWrapper("", "", CxlRejResponseTo.OrderCancelRequest, CxlRejReason.UnknownOrder,
                        string.Format("CRITICAL ERROR @CancelOrderThread:{0}", ex.Message));
            }

        }

        protected void CancelAllOrdersThread(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper)param;

                CancelAllOrders();
            }
            catch (Exception ex)
            {
                Wrapper rejWrapper = new OrderCancelRejectWrapper("", "", CxlRejResponseTo.OrderCancelRequest, CxlRejReason.UnknownOrder,
                        string.Format("CRITICAL ERROR @CancelAllOrdersThread:{0}", ex.Message));
            }
        }

        protected Order DoGetActiveOrder(string clOrdId)
        {
            if (OrderIdsMapper.ContainsKey(clOrdId))
            {
                string orderId = OrderIdsMapper[clOrdId];

                if (ActiveOrders.ContainsKey(orderId))
                {

                    Order order = ActiveOrders[orderId];
                    return order;
                }
                else
                    return null;
            }
            else
                return null;
        }

        protected Wrapper DoCancel(string clOrdId, bool cancel)
        {
            List<Wrapper> toPublish = new List<Wrapper>();

            if (OrderIdsMapper.ContainsKey(clOrdId))
            {
                string orderId = OrderIdsMapper[clOrdId];

                if (ActiveOrders.ContainsKey(orderId))
                {

                    Order order = ActiveOrders[orderId];
                    DoLog(string.Format("Running cancellation for orderId {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);
                    CancelOrderResponse cxlResp = IOLOrderRouterManager.Cancel(order);

                    if (cxlResp.ok.HasValue && cxlResp.ok.Value)
                    {
                        DoLog(string.Format("Cancellation requested for orderId {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);

                        return null;
                    }
                    else
                    {
                        OrderCancelRejectWrapper rejWrapper = new OrderCancelRejectWrapper(clOrdId, orderId,
                                                                                            cancel ? CxlRejResponseTo.OrderCancelRequest : CxlRejResponseTo.OrderCancelReplaceRequest,
                                                                                            CxlRejReason.UnknownOrder,
                                                                                            string.Format("Cancellation rejected for order Id {0}:{1}", orderId, cxlResp.GetError()));
                        return rejWrapper;
                    }
                }
                else
                {
                    OrderCancelRejectWrapper rejWrapper = new OrderCancelRejectWrapper(clOrdId, orderId,
                                                                                        cancel ? CxlRejResponseTo.OrderCancelRequest : CxlRejResponseTo.OrderCancelReplaceRequest,
                                                                                        CxlRejReason.UnknownOrder,
                                                                                        string.Format("Unknown order for order Id {0}", orderId));
                    return rejWrapper;
                }
            }
            else
            {
                OrderCancelRejectWrapper rejWrapper = new OrderCancelRejectWrapper(clOrdId, "",
                                                                        cancel ? CxlRejResponseTo.OrderCancelRequest : CxlRejResponseTo.OrderCancelReplaceRequest,
                                                                        CxlRejReason.UnknownOrder,
                                                                        string.Format("Unknown order for client order Id {0}", clOrdId));
                return rejWrapper;
            }
        }

        protected void CancelOrder(Wrapper wrapper, bool cancel)
        {
            string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);
            Wrapper rejWrapper=null;

            try
            {
                lock (tLock)
                {
                    rejWrapper = DoCancel(clOrdId, cancel);

                    DoLog(string.Format("Order with ClOrdId {0} successfully cancelled", clOrdId), Main.Common.Util.Constants.MessageType.Information);
                }

                if (rejWrapper != null)
                    OnMessageRcv(rejWrapper);
            }
            catch (Exception ex)
            {
                string msg = string.Format("Exception cancelling client order Id {0}:{1}", clOrdId, ex.Message);

                DoLog(msg, Main.Common.Util.Constants.MessageType.Error);

                OrderCancelRejectWrapper exRejWrapper = new OrderCancelRejectWrapper(clOrdId, "",
                                                                            cancel ? CxlRejResponseTo.OrderCancelRequest : CxlRejResponseTo.OrderCancelReplaceRequest,
                                                                            CxlRejReason.UnknownOrder,msg);

                OnMessageRcv(exRejWrapper);
            }
        }


        #endregion

        #region Public  Methods

        public override CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("Routing with Invertir Online to market for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    RouteNewOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("Updating order with Invertir Online  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                   
                    new Thread(UpdateOrderThread).Start(wrapper);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("Canceling order with Invertir Online  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    new Thread(CancelOrderThread).Start(wrapper);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders @ Invertir Online", IOLConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                    new Thread(CancelAllOrdersThread).Start(wrapper);
                    
                }
                else
                {
                    DoLog(string.Format("Could not process order routing for action {0} with Invertir Online:", wrapper.GetAction().ToString()),
                          Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("Could not process order routing for action {0} with Invertir Online:", wrapper.GetAction().ToString())));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog("Error processing market instruction @Invertir Online order router:" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLock = new object();

                    OrderConverter = new OrderConverter();

                    ActiveOrders = new Dictionary<string, Order>();
                    OrderIdsMapper = new Dictionary<string, string>();
                    LatestExecReport = new Dictionary<string, ExecutionReportResp>();
                    PendingUpdate = new Dictionary<string, Order>();
                    
                    IOLOrderRouterManager = new IOLOrderRouterManager(pOnLogMsg,IOLConfiguration.AccountNumber,IOLConfiguration.ConfigConnectionString,
                                                                      IOLConfiguration.MainURL);


                    new Thread(EvalExecutionReportsThread).Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        protected override CMState ProcessIncoming(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No incoming module set for Invertir Online order router!"));
        }

        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No outgoing module set for Invertir Online order router!"));
        }

        #endregion
    }
}

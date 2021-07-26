using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouter.Mock.Common;
using zHFT.OrderRouter.Mock.Common.Configuration;
using zHFT.OrderRouter.Mock.Common.Converters;
using zHFT.OrderRouter.Mock.Common.Wrappers;


namespace zHFT.OrderRouters.Mock
{
    public class OrderRouter : OrderRouterBase, ILogger
    {
        #region Protected Attributes

        protected int InternalOrderId { get; set; }

        protected IConfiguration Config { get; set; }

        protected Configuration Configuration
        {
            get { return (Configuration)Config; }
            set { Config = value; }
        }

        protected OrderConverter OrderConverter { get; set; }

        protected List<Order> PendingToExecuteOrders { get; set; }
        
        protected  object tLock { get; set; }

        #endregion

        #region Protected Methods

        protected void FillOrders(object param)
        {

            while (true)
            {
                lock (tLock)
                {
                    List<Order> toRemove = new List<Order>();
                    foreach (Order order in PendingToExecuteOrders)
                    {
                        TimeSpan elapsed = DateTime.Now - order.EffectiveTime.Value;
                        if (elapsed.TotalSeconds > Configuration.OrdeExecutionEveryNSeconds)
                        {
                            //We send the executed ER
                            order.OrderId = InternalOrderId.ToString();
                            ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(ExecType.Trade, OrdStatus.Filled,
                                                                                           0, order.OrderQty.Value, order.Price, order.Price,order.OrderQty, order);
                            toRemove.Add(order);
                            OnMessageRcv(erWrapper);
                        }
                    }

                    toRemove.ForEach(x => PendingToExecuteOrders.Remove(x));
                }
                Thread.Sleep(1000);//1 seconds
            }
        }

        protected void RouteNewOrder(object pWrapper)
        {
            Wrapper wrapper = (Wrapper) pWrapper;
            lock (tLock)
            {
                Order newOrder = OrderConverter.ConvertNewOrder(wrapper);
                newOrder.EffectiveTime = DateTime.Now;

                PendingToExecuteOrders.Add(newOrder);
            }
        }

        protected void UpdateOrder(object pWrapper)
        {
            Wrapper wrapper = (Wrapper) pWrapper;
            lock (tLock)
            {
                string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);
                string origClOrdId = (string)wrapper.GetField(OrderFields.OrigClOrdID);
                double? ordQty = (double?)wrapper.GetField(OrderFields.OrderQty);
                double? price = (double?)wrapper.GetField(OrderFields.Price);

               
                Order pendingOrder = PendingToExecuteOrders.Where(x => x.ClOrdId == origClOrdId).FirstOrDefault();

                if (pendingOrder != null)
                {
                    pendingOrder.OrderQty = ordQty;
                    pendingOrder.Price = price;
                    pendingOrder.ClOrdId = clOrdId;

                    ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(ExecType.Replaced, OrdStatus.Replaced,
                                                                                  0, pendingOrder.OrderQty.Value, null, null,null, pendingOrder);

                    OnMessageRcv(erWrapper);
                }
                else
                {

                    ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(ExecType.Rejected, OrdStatus.Rejected,
                                                                  0, 0, null, null,null, new Order() { }, string.Format("No such order active for ClOrdId {0}", origClOrdId));

                    OnMessageRcv(erWrapper);

                }


            }
            
        }

        protected void CancelOrder(object pWrapper)
        {
            Wrapper wrapper = (Wrapper) pWrapper;
            string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);
            string origClOrdId = (string)wrapper.GetField(OrderFields.OrigClOrdID);

            lock (PendingToExecuteOrders)
            {
                Order cancelOrder = PendingToExecuteOrders.Where(x => x.ClOrdId == origClOrdId).FirstOrDefault();


                if (cancelOrder != null)
                {

                    ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(ExecType.Canceled, OrdStatus.Canceled,
                                                                                   0, cancelOrder.OrderQty.Value, null, null,null, cancelOrder);

                    OnMessageRcv(erWrapper);
                
                }
                else
                {

                    ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(ExecType.Rejected, OrdStatus.Rejected,
                                                                  0, 0, null, null,null, new Order() { }, string.Format("No such order active for ClOrdId {0}", origClOrdId));

                    OnMessageRcv(erWrapper);

                }
            
            }
        }

        protected void CancelAllOrders(object pParam)
        {
            lock (PendingToExecuteOrders)
            {
                foreach (Order order in PendingToExecuteOrders)
                {

                    ExecutionReportWrapper erWrapper = new ExecutionReportWrapper(ExecType.Canceled, OrdStatus.Canceled,
                                                                                  0, order.OrderQty.Value, null, null,null, order);

                    OnMessageRcv(erWrapper);

                }

                PendingToExecuteOrders.Clear();
            }
        }

        #endregion

        #region Public Methods

        public  void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {

                    if (wrapper.GetAction() == Actions.NEW_ORDER)
                    {
                        DoLog(string.Format("@{0}:Routing with Mock to market for symbol {1}", Configuration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        Thread RouteOrdersThread = new Thread(new ParameterizedThreadStart(RouteNewOrder));
                        RouteOrdersThread.Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                    {
                        DoLog(string.Format("@{0}:Updating order with Mock  for symbol {1}", Configuration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        
                        Thread RouteOrdersThread = new Thread(new ParameterizedThreadStart(UpdateOrder));
                        RouteOrdersThread.Start(wrapper);
                        return CMState.BuildSuccess();

                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                    {
                        DoLog(string.Format("@{0}:Canceling order with Mock  for ClOrdId {1}", Configuration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        
                        Thread RouteOrdersThread = new Thread(new ParameterizedThreadStart(CancelOrder));
                        RouteOrdersThread.Start(wrapper);
                        return CMState.BuildSuccess();
                        
                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                    {
                        DoLog(string.Format("@{0}:Cancelling all active orders @ Mock", Configuration.Name), Main.Common.Util.Constants.MessageType.Information);
                        Thread RouteOrdersThread = new Thread(new ParameterizedThreadStart(CancelAllOrders));
                        RouteOrdersThread.Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else
                    {

                        Actions action = wrapper.GetAction();
                        DoLog(string.Format("@{0}:Sending message " + action + " not implemented", Configuration.Name), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message " + action + " not implemented", Configuration.Name)));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");

            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Main.Common.Util.Constants.MessageType.Error);
                throw;
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {

            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                OrderConverter = new OrderConverter();
                PendingToExecuteOrders = new List<Order>();
                
                tLock=new object();

                InternalOrderId = 1;

                Thread RouteOrdersThread = new Thread(new ParameterizedThreadStart(FillOrders));
                RouteOrdersThread.Start();

                DoLog("Mock Order Router Initializing... ", Main.Common.Util.Constants.MessageType.Information);
            }
            return true;
        }
        #endregion
    }
}

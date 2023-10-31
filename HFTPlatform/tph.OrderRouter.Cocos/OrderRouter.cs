using System;
using System.Collections.Generic;
using System.Threading;
using tph.OrderRouter.Cocos.Common.Converters;
using tph.OrderRouter.Cocos.Common.DTO.Accounts;
using tph.OrderRouter.Cocos.Common.DTO.Orders;
using tph.OrderRouter.ServiceLayer;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common;
using zHFT.OrderRouters.Common.Wrappers;
using zHFT.OrderRouters.Cryptos;

namespace tph.OrderRouter.Cocos
{
    public class OrderRouter: OrderRouterBase,ICommunicationModule
    {
        #region Protected Attributes
        
        protected  object tLock { get; set; }
        
        protected Dictionary<string,Order> ActiveOrders { get; set; }
        
        protected CocosOrderRouterServiceClient CocosOrderRouterServiceClient { get; set; }
        
        protected tph.OrderRouter.Cocos.Common.Configuration CocosConfiguration
        {
            get { return (tph.OrderRouter.Cocos.Common.Configuration)Config; }
            set { Config = value; }
        }
        
        #endregion
        
        #region Protected Methods
        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            Config = new  tph.OrderRouter.Cocos.Common.Configuration().GetConfiguration< tph.OrderRouter.Cocos.Common.Configuration>(configFile, noValueFields);
        }

        protected override CMState ProcessIncoming(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No incoming module set for Cocos order router!"));
        }

        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No outgoing module set for Cocos order router!"));
        }

        protected void TestValidateNewOrder()
        {
            Order order= new Order();
            
            order.Security=new Security();;
            order.Security.Symbol = "GGAL";
            order.Security.SecType = SecurityType.CS;
            order.Security.Exchange = "BUE";
            

            order.OrderQty = 10;
            order.Price = 100;
            order.TimeInForce = TimeInForce.Day;
            order.Side = Side.Buy;
            
            CocosOrderRouterServiceClient.ValidateNewOrder(order);
            
            CocosOrderRouterServiceClient.ConfirmNewOrder(order);
        }

        protected void TestSyncPositions()
        {
            Positions pos = CocosOrderRouterServiceClient.GetPositions("20171");
            //GetPositions
        }

        protected void DoRoute(Order order)
        {
            //TODO: Send order and return pending new ER
            DoLog(string.Format("New order created for cl.order Id {0}",  order.ClOrdId),Constants.MessageType.Information);
            
        }
        
        protected  void EvalExecutionReportsThread(object param)
        {

            try
            {
                while (true)
                {
                    lock (tLock)
                    {
                        ExecutionReportsResponse execResp = CocosOrderRouterServiceClient.GetExecutionReports("20171");

                        zHFT.Main.BusinessEntities.Orders.ExecutionReport[] execReports =  ExecutionReportConverter.ConvertExecutionReports(execResp);
                        
                        List<string> ordersToDel = new List<string>();
                        foreach (Order order in ActiveOrders.Values)
                        {
                            try
                            {
                                
                                //TODO: Retrieve execution report manager to retrieve execution reports
                                //TODO: Pendings Update dictionary
                                //TODO: ProcessExecutionReport thread
                                //TODO: LatestExecReport
                                //ExecutionReportResp execReport = IOLOrderRouterManager.GetExecutionReport(order.OrderId);

//                                if (!PendingUpdate.ContainsKey(order.OrderId.ToString()))
//                                {
//                                    ExecutionReportWrapper execReportWrapper = new ExecutionReportWrapper(order, execReport);
//                                    new Thread(ProcessExecutionReport).Start(execReportWrapper);
//
//                                    if (!execReport.IsOpenOrder())
//                                        ordersToDel.Add(order.OrderId.ToString());
//
//                                    if (!LatestExecReport.ContainsKey(order.ClOrdId))
//                                        LatestExecReport.Add(order.ClOrdId, execReport);
//                                    else
//                                        LatestExecReport[order.ClOrdId] = execReport;
//                                }//TODO eval Pendingreplace ER on ELSE
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("Critical error evaluating execution report for orderId {0}!: {1}",order.OrderId, ex.Message), Constants.MessageType.Error);
                            }
                        }

                        ordersToDel.ForEach(x => ActiveOrders.Remove(x));
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error evaluating execution reports!: {0}", ex.Message), Constants.MessageType.Error);

            }
        
        }
        
        protected void RouteNewOrder(Wrapper wrapper)
        {
            Order order = null;
            try
            {
                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                lock (tLock)
                {
                    order = OrderConverter.GetNewOrder(wrapper);
                }

                DoLog(string.Format("Routing Client Order Id {0}", order.ClOrdId), Constants.MessageType.Information);

                DoRoute(order);

                lock (tLock)
                {
                    ActiveOrders.Add(order.OrderId.ToString(), order);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error routing order {0} to the exchange!: {1}",order.ClOrdId, ex.Message), Constants.MessageType.Error);

                RejectedExecutionReportWrapper rejectedWrapper = new RejectedExecutionReportWrapper(order, ex.Message);

                new Thread(ProcessExecutionReport).Start(rejectedWrapper);
            }
        }

        #endregion

        public  CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("Routing with Cocos to market for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()),Constants.MessageType.Information);
                    RouteNewOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("Updating order with Cocos for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Constants.MessageType.Information);
                   
                    //new Thread(UpdateOrderThread).Start(wrapper);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("Canceling order with Cocos for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Constants.MessageType.Information);
                    //new Thread(CancelOrderThread).Start(wrapper);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders @ Cocos", CocosConfiguration.Name), Constants.MessageType.Information);
                    //new Thread(CancelAllOrdersThread).Start(wrapper);
                    
                }
                else
                {
                    DoLog(string.Format("Could not process order routing for action {0} with Cocos:", wrapper.GetAction().ToString()),Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("Could not process order routing for action {0} with Cocos:", wrapper.GetAction().ToString())));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog("Error processing market instruction @Invertir Online order router:" + ex.Message, Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        
        }


        public  bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLock = new object();

                    //OrderConverter = new OrderConverter();

                    ActiveOrders = new Dictionary<string, Order>();

                    CocosOrderRouterServiceClient = new CocosOrderRouterServiceClient(CocosConfiguration.BaseURL,
                                                                                      CocosConfiguration.DNI, 
                                                                                      CocosConfiguration.User, 
                                                                                      CocosConfiguration.Password);

                    
                    //TestValidateNewOrder();
                    
                    new Thread(EvalExecutionReportsThread).Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile,Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critical ERROR initializing " + configFile + ":" + ex.Message,Constants.MessageType.Error);
                return false;
            }
        }
    }
}
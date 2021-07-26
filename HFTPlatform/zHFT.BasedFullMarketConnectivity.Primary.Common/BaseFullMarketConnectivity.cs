using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;
using zHFT.OrderRouters.Primary.Common;
using zHFT.OrderRouters.Primary.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
using ExecType = QuickFix.ExecType;

namespace zHFT.BasedFullMarketConnectivity.Primary.Common
{
    public abstract class BaseFullMarketConnectivity : Application
    {
        #region Private  Consts

        protected int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        protected int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        protected string _DUMMY_SECURITY = "kcdlsncslkd";

        protected string _MAIN_EXCHANGE = "ROFX";

        #endregion

        #region Protected Attributes

        protected IFIXMessageCreator FIXMessageCreator { get; set; }
        protected SessionSettings SessionSettings { get; set; }
        protected FileStoreFactory FileStoreFactory { get; set; }
        protected ScreenLogFactory ScreenLogFactory { get; set; }
        protected SessionID SessionID { get; set; }
        protected MessageFactory MessageFactory { get; set; }
        protected SocketInitiator Initiator { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected object tLock = new object();

        protected object tLockSavingMarketData = new object();

        protected int MarketDataRequestId { get; set; }

        protected int OrderIndexId { get; set; }

        protected OrderConverter OrderConverter { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }

        protected Dictionary<string, Order> ActiveOrders { get; set; }

        protected Dictionary<string, string> ActiveOrderIdMapper { get; set; }

        protected Dictionary<string, string> ReplacingActiveOrderIdMapper { get; set; }

        protected Dictionary<string, zHFT.Main.Common.Enums.SecurityType> SecurityTypes { get; set; }

        protected Dictionary<int, Security> ActiveSecurities { get; set; }

        #endregion

        #region abstract Methods

        public abstract BaseConfiguration GetConfig();

        protected abstract void ProcessSecurities(SecurityList securityList);

        protected abstract void CancelMarketData(Security sec);

        #endregion

        #region abstract QuickFix Methods
        public abstract void fromApp(Message value, SessionID sessioId);
        public abstract void toAdmin(Message value, SessionID sessioId);
        #endregion

        #region Quickfix Methods

        public virtual void fromAdmin(Message value, SessionID sessionId)
        {
            DoLog("Invocación de fromAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
        }

        public virtual void onCreate(SessionID value)
        {
            DoLog("Invocación de onCreate : " + value.ToString(), Constants.MessageType.Information);
        }

        public virtual void onLogon(SessionID value)
        {
          
            SessionID = value;
            DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

            if (SessionID != null)
                DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);
            else
                DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);
            
        }

        public virtual void onLogout(SessionID value)
        {
            SessionID = null;
            DoLog("Invocación de onLogout : " + value.ToString(), Constants.MessageType.Information);
        }

        public virtual void toApp(Message value, SessionID sessionId)
        {
            DoLog("Invocación de toApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
        }

        #endregion

        #region Protected Methods

        protected void ProcessNewOrderExecutionReport(string clOrdId,ExecutionReportWrapper erWrapper)
        {
            zHFT.Main.Common.Enums.ExecType execType = (zHFT.Main.Common.Enums.ExecType)erWrapper.GetField(ExecutionReportFields.ExecType);
            zHFT.Main.Common.Enums.OrdStatus ordStatus = (zHFT.Main.Common.Enums.OrdStatus)erWrapper.GetField(ExecutionReportFields.OrdStatus);
            
            if (ActiveOrders.Keys.Contains(clOrdId))
            {
                if (execType == zHFT.Main.Common.Enums.ExecType.New)//New-> Guardamos el OrderId de mercado
                {
                    Order order = ActiveOrders[clOrdId];
                    string orderId = (string)erWrapper.GetField(ExecutionReportFields.OrderID);
                    order.OrderId = orderId;
                }
                else if (execType == zHFT.Main.Common.Enums.ExecType.Trade && ordStatus == zHFT.Main.Common.Enums.OrdStatus.Filled)
                {
                    ActiveOrders.Remove(clOrdId);
                    ActiveOrderIdMapper.Remove(clOrdId);
                }
                else if (execType == zHFT.Main.Common.Enums.ExecType.DoneForDay || execType == zHFT.Main.Common.Enums.ExecType.Stopped
                                                                                || execType == zHFT.Main.Common.Enums.ExecType.Suspended || execType == zHFT.Main.Common.Enums.ExecType.Rejected
                                                                                || execType == zHFT.Main.Common.Enums.ExecType.Expired || execType == zHFT.Main.Common.Enums.ExecType.Canceled)
                {

                    ActiveOrders.Remove(clOrdId);
                    ActiveOrderIdMapper.Remove(clOrdId);
                }
                    
                // El formato de los ClOrdId/OrigClOrdId @Primary es distinto al manejado por el OrderRouter
                // entonces hay un mapeo entre lo que se manda y lo que se recibe
                erWrapper.ClOrdId = clOrdId;
                erWrapper.OrigClOrdId = null;
                    
            }
            else
            {
                DoLog(string.Format("@{0} Could not find order for ClOrderId {1} ", GetConfig().Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
            }
            
        }

        protected void ProcessCancelReplaceExecutionReport(string clOrdId,string origClOrdId,ExecutionReportWrapper erWrapper)
        {
            zHFT.Main.Common.Enums.ExecType execType = (zHFT.Main.Common.Enums.ExecType)erWrapper.GetField(ExecutionReportFields.ExecType);
            zHFT.Main.Common.Enums.OrdStatus ordStatus = (zHFT.Main.Common.Enums.OrdStatus)erWrapper.GetField(ExecutionReportFields.OrdStatus);
            
            string oldMarketClOrderId = (string)erWrapper.GetField(ExecutionReportFields.OrigClOrdID);
            string newMarketClOrderIdRequested = (string)erWrapper.GetField(ExecutionReportFields.ClOrdID);
            
            origClOrdId = oldMarketClOrderId != null ? ActiveOrderIdMapper.Keys.Where(x => ActiveOrderIdMapper[x]== oldMarketClOrderId).FirstOrDefault() : null;
            clOrdId = newMarketClOrderIdRequested != null ? ReplacingActiveOrderIdMapper.Keys.Where(x => ReplacingActiveOrderIdMapper[x] == newMarketClOrderIdRequested).FirstOrDefault() : null;
            
            if (!string.IsNullOrEmpty(origClOrdId) && ActiveOrders.Keys.Contains(origClOrdId))
            {

                if (execType == zHFT.Main.Common.Enums.ExecType.Replaced)
                {
                    Order order = ActiveOrders[origClOrdId];
                    string orderId = (string)erWrapper.GetField(ExecutionReportFields.OrderID);
                    order.OrderId = orderId;

                    ActiveOrders.Remove(origClOrdId);
                    ActiveOrders.Add(clOrdId, order);
                    ReplacingActiveOrderIdMapper.Remove(clOrdId);
                    ActiveOrderIdMapper.Remove(origClOrdId);
                    ActiveOrderIdMapper.Add(clOrdId, newMarketClOrderIdRequested);
                }
                else if (execType == zHFT.Main.Common.Enums.ExecType.Canceled)
                {
                    ActiveOrders.Remove(origClOrdId);
                    ActiveOrderIdMapper.Remove(origClOrdId);
                }
                
                // El formato de los ClOrdId/OrigClOrdId @Primary es distinto al manejado por el OrderRouter
                // entonces hay un mapeo entre lo que se manda y lo que se recibe
                erWrapper.ClOrdId = clOrdId;
                erWrapper.OrigClOrdId = origClOrdId;
            }
            else
            {
                DoLog(string.Format("@{0} Ignoring unknown ClOrderId {1}  for Execution Report!", GetConfig().Name,oldMarketClOrderId), Main.Common.Util.Constants.MessageType.Information);
                //return null;
            }
        }

        protected ExecutionReportWrapper ProcesssExecutionReportMessage(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", GetConfig().Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            ExecutionReportWrapper erWrapper = new ExecutionReportWrapper((QuickFix50.ExecutionReport)message, GetConfig());

            string marketClOrderId = (string)erWrapper.GetField(ExecutionReportFields.ClOrdID);
           
            zHFT.Main.Common.Enums.ExecType execType = (zHFT.Main.Common.Enums.ExecType)erWrapper.GetField(ExecutionReportFields.ExecType);
            zHFT.Main.Common.Enums.OrdStatus ordStatus = (zHFT.Main.Common.Enums.OrdStatus)erWrapper.GetField(ExecutionReportFields.OrdStatus);

            string clOrdId = marketClOrderId != null ? ActiveOrderIdMapper.Keys.Where(x => ActiveOrderIdMapper[x].ToString() == marketClOrderId).FirstOrDefault() : null;
            string origClOrdId = null;
            
            if (clOrdId != null)//Estamos en un alta
            {
                ProcessNewOrderExecutionReport(clOrdId, erWrapper);
            }
            else //Estamos en un update/cancel
            {
                ProcessCancelReplaceExecutionReport(clOrdId, origClOrdId, erWrapper);
            }

            return erWrapper;
        }

        protected CMState ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                lock (tLock)
                {
                    SecurityList securityList = SecurityListConverter.GetSecurityList(wrapper, GetConfig());

                    ProcessSecurities(securityList);
                    DoLog(string.Format("@{0}:Processing Security List ",GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);
                    return CMState.BuildSuccess();
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error Publishing Security List. Error={1} ",
                                                GetConfig().Name, ex.Message),Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected void DoProcessSecurityListRequest(object param)
        {
            try
            {
                QuickFix.Message rq = (QuickFix.Message)param;
                Session.sendToTarget(rq, SessionID);
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error Processing Security List Reuqest. Error={1} ",
                                    GetConfig().Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            
            }
        
        }

        protected CMState ProcessSecurityListRequest(Wrapper wrapper)
        {
            if (SessionID != null)
            {
                try
                {
                    zHFT.Main.Common.Enums.SecurityListRequestType type = (zHFT.Main.Common.Enums.SecurityListRequestType)wrapper.GetField(SecurityListRequestField.SecurityListRequestType);

                    if (type == zHFT.Main.Common.Enums.SecurityListRequestType.AllSecurities)
                    {
                        QuickFix.Message rq = FIXMessageCreator.RequestSecurityList((int)type, _DUMMY_SECURITY);

                        Thread secListReqThrad = new Thread(DoProcessSecurityListRequest);
                        secListReqThrad.Start(rq);

                        return CMState.BuildSuccess();
                    }
                    else
                        throw new Exception(string.Format("@{0} SecurityListRequestType not implemented: {1}", GetConfig().Name, type.ToString()));
                }
                catch (Exception ex)
                {
                    return CMState.BuildFail(ex);
                }
            }
            else
            {
                DoLog(string.Format("@{0}:Session not initialized on security list request ", GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildSuccess();
            }
        }

        protected void DoRunMarketDataRequest(object param)
        {
            QuickFix.Message msg = (QuickFix.Message)param;

            try
            {
                DoLog(string.Format("@{0}:Sending Market Data Request:{1} ", GetConfig().Name, msg.ToString()), Main.Common.Util.Constants.MessageType.Information);
                Session.sendToTarget(msg, SessionID);
                DoLog(string.Format("@{0}:Market Data Request Successfully Sent ", GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);

            }
            catch (Exception ex)
            {

                DoLog(string.Format("@{0}:Error sending market data request: {1}! ", GetConfig().Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }

        }

        protected CMState ProcessMarketDataRequest(Wrapper marketDataRequestWrapper)
        {
            if (SessionID != null)
            {
                MarketDataRequest rq = MarketDataRequestConverter.GetMarketDataRequest(marketDataRequestWrapper);

                if (rq.SubscriptionRequestType == zHFT.Main.Common.Enums.SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketData(rq.Security);

                    //Obs: No anda bien el unsubscribe de Primary así que simplemente nos limitamos a sacar el activo de la
                    //list de ActiveSecurities
                    //QuickFix.Message msg = FIXMessageCreator.RequestMarketData(MarketDataRequestId, rq.Security.Symbol, rq.SubscriptionRequestType);
                    //Session.sendToTarget(msg, SessionID);

                    return CMState.BuildSuccess();
                }
                else
                {
                    QuickFix.Message msg = FIXMessageCreator.RequestMarketData(MarketDataRequestId, rq.Security.Symbol, rq.SubscriptionRequestType);
                    MarketDataRequestId++;

                    if (!SecurityTypes.ContainsKey(rq.Security.AltIntSymbol))
                    {
                        if (!ExchangeConverter.IsFullSymbol(rq.Security.AltIntSymbol))
                        {
                            SecurityTypes.Add(rq.Security.GetFullSymbol(), rq.Security.SecType);
                            ActiveSecurities.Add(rq.ReqId, new Security() { Symbol = rq.Security.GetFullSymbol(), Active = true });
                        }
                        else
                        {
                            SecurityTypes.Add(rq.Security.AltIntSymbol, rq.Security.SecType);
                            ActiveSecurities.Add(rq.ReqId, new Security() { Symbol = rq.Security.AltIntSymbol, Active = true });
                        }
                   
                    }
                    Thread mdRqThrad = new Thread(DoRunMarketDataRequest);
                    mdRqThrad.Start(msg);
                }

                return CMState.BuildSuccess();

            }
            else
            {
                DoLog(string.Format("@{0}:Session not initialized on new market data request ", GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildSuccess();
            }
        }

        protected int GetNextOrderId()
        {

            DateTime dayStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

            TimeSpan span = DateTime.Now - dayStart;

            return Convert.ToInt32(span.TotalSeconds );
        
        }

        protected CMState RouteNewOrder(Wrapper wrapper)
        {
            string marketClOrdId = "";
            try
            {
                if (SessionID != null)
                {
                    
                    Order newOrder = OrderConverter.ConvertNewOrder(wrapper);
                    newOrder.EffectiveTime = DateTime.Now;

                    lock (tLock)
                    {
                        marketClOrdId = (OrderIndexId * 100).ToString();
                        OrderIndexId++;
                    }

                    double orderQty = newOrder.OrderQty.Value;
                    //Procesamientos especiales de la cantidad de las ordenes
                    //if (newOrder.Security.SecType == zHFT.Main.Common.Enums.SecurityType.TB)
                    //{
                    //    orderQty *= 1000;
                    //}

                    QuickFix.Message msg = FIXMessageCreator.CreateNewOrderSingle(marketClOrdId, 
                                                                                    newOrder.Symbol, 
                                                                                    newOrder.Side, 
                                                                                    newOrder.OrdType,
                                                                                    newOrder.SettlType, 
                                                                                    newOrder.TimeInForce, 
                                                                                    newOrder.EffectiveTime.Value,
                                                                                    orderQty, 
                                                                                    newOrder.Price,
                                                                                    newOrder.StopPx, 
                                                                                    newOrder.Account);
                    
                    DoLog(string.Format("Sending new order {0} for symbol {1} to the exchange",marketClOrdId,newOrder.Symbol),Constants.MessageType.Information);


                    Thread newOrdThread = new Thread(DoRunNewOrder);
                    newOrdThread.Start(msg);

                    ActiveOrders.Add(newOrder.ClOrdId, newOrder);
                    ActiveOrderIdMapper.Add(newOrder.ClOrdId,marketClOrdId);
                   
                    return CMState.BuildSuccess();
                }
                else
                {
                    DoLog(string.Format("@{0}:Session not initialized on new order ", GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildSuccess();
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error sending order {0} to the exchange:{1}",marketClOrdId,ex.Message),Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected void DoUpdateOrder(object param)
        {
            QuickFix.Message updMessage = (QuickFix.Message)param;
            try
            {
                Session.sendToTarget(updMessage, SessionID);
                DoLog(string.Format("@{0}:Update Message Thread: Message succesfully sent: {1}! ", GetConfig().Name, updMessage.ToString()), Main.Common.Util.Constants.MessageType.Information);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error updating message {1}: {2}! ", GetConfig().Name, updMessage.ToString(),ex.Message), Main.Common.Util.Constants.MessageType.Error);

            }
        }

        protected CMState UpdateOrder(Wrapper wrapper)
        {
            try
            {
                if (SessionID != null)
                {
                    string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);
                    string origClOrdId = (string)wrapper.GetField(OrderFields.OrigClOrdID);
                    double? ordQty = (double?)wrapper.GetField(OrderFields.OrderQty);
                    double? price = (double?)wrapper.GetField(OrderFields.Price);

                    if (ActiveOrders.Keys.Contains(origClOrdId))
                    {
                        Order order = ActiveOrders[origClOrdId];
                        string marketOrderId = ActiveOrderIdMapper[origClOrdId];

                        string newMarketOrderIdRequested = (Convert.ToInt32(marketOrderId) + 1).ToString();
                        ReplacingActiveOrderIdMapper.Add(clOrdId, newMarketOrderIdRequested);

                        order.ClOrdId = clOrdId;
                        order.OrigClOrdId = origClOrdId;

                        ordQty = order.OrderQty;//We use the old qty

                        //CancelOrder(order, origClOrdId, newMarketOrderIdRequested.ToString());
                        //RouteNewOrder(order, (newMarketOrderIdRequested + 1).ToString());

                       
                        //Procesamientos especiales de la cantidad de las ordenes
                        if (order.Security.SecType == zHFT.Main.Common.Enums.SecurityType.TB)
                        {
                            ordQty *= 1000;
                        }

                        
                        QuickFix.Message updMessage = FIXMessageCreator.CreateOrderCancelReplaceRequest(
                                                                            newMarketOrderIdRequested.ToString(),
                                                                            order.OrderId,
                                                                            marketOrderId.ToString(),
                                                                            order.Security.Symbol,
                                                                            order.Side,
                                                                            order.OrdType,
                                                                            order.SettlType,
                                                                            order.TimeInForce,
                                                                            order.EffectiveTime.Value,
                                                                            ordQty,//qty to update,
                                                                            price,//price to update
                                                                            order.StopPx,
                                                                            order.Account
                                                                            );

                        //Session.sendToTarget(updMessage, SessionID);
                        Thread updThread = new Thread(DoUpdateOrder);
                        updThread.Start(updMessage);
                        return CMState.BuildSuccess();

                    }
                    else
                    {
                        DoLog(string.Format("@{0}:Order for ClOrdId {1} not found! ", GetConfig().Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
                        throw new Exception(string.Format("@{0}:Order for ClOrdId {1} not found!!", GetConfig().Name, clOrdId));
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Session not initialized on update order ", GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildSuccess();
                }

            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }

        }

        protected virtual void DoRunNewOrder(object param)
        {
            QuickFix.Message nosMessage = (QuickFix.Message)param;

            try
            {
                DoLog(string.Format("@{0}:Sending New Order Message Thread: {1}! ", GetConfig().Name, nosMessage.ToString()), Main.Common.Util.Constants.MessageType.Information);
                Session.sendToTarget(nosMessage, SessionID);
                DoLog(string.Format("@{0}:New Order Message Thread: Message succesfully sent: {1}! ", GetConfig().Name, nosMessage.ToString()), Main.Common.Util.Constants.MessageType.Information);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error sending new order message {1}: {2}! ", GetConfig().Name, nosMessage.ToString(), ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void DoCancel(object param)
        {
            QuickFix.Message cancelMessage = (QuickFix.Message)param;
            try
            {
                Session.sendToTarget(cancelMessage, SessionID);
                DoLog(string.Format("@{0}:Cancel Message Thread: Message succesfully sent: {1}! ", GetConfig().Name, cancelMessage.ToString()), Main.Common.Util.Constants.MessageType.Information);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelling message {1}: {2}! ", GetConfig().Name, cancelMessage.ToString(), ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void DoCancelAllOrders(object param)
        {
            try
            {

                lock (tLock)
                {
                    foreach (Order order in ActiveOrders.Values.Where(x => x.Security.Active))
                    {
                        ExecCancel(order);

                    }
                }

                //QuickFix.Message massiveCancelMsg = (QuickFix.Message)param;
                //Session.sendToTarget(massiveCancelMsg, SessionID);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelling all orders: {1}! ", GetConfig().Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        
        }

        protected CMState RequestOrderMassStatus()
        {
            TimeSpan epoch = DateTime.Now - new DateTime(1970, 1, 1);
            
            QuickFix.Message massStatusReqMsg = FIXMessageCreator.CreateOrderMassStatusRequest(epoch.TotalSeconds.ToString());
            
            Session.sendToTarget(massStatusReqMsg, SessionID);
            
            return CMState.BuildSuccess();
        }

        protected CMState CancelAllOrders()
        {
            QuickFix.Message massiveCancelMsg = FIXMessageCreator.CreateOrderMassCancelRequest();

            Thread cancelThread = new Thread(DoCancelAllOrders);
            cancelThread.Start(massiveCancelMsg);

            return CMState.BuildSuccess();

        }

        protected void ExecCancel(Order order)
        {

            string clOrdId = order.ClOrdId;

            if (ActiveOrderIdMapper.Keys.Contains(clOrdId))
            {

                string marketOrderId = ActiveOrderIdMapper[clOrdId];

                string newMarketOrderIdRequested = (Convert.ToInt32(marketOrderId) + 1).ToString();
                ReplacingActiveOrderIdMapper.Add(clOrdId, newMarketOrderIdRequested);

                order.ClOrdId = (Convert.ToInt32(clOrdId) + 1).ToString();
                order.OrigClOrdId = clOrdId;
                
                //ActiveOrderIdMapper.Add(order.ClOrdId,newMarketOrderIdRequested);

                double orderQty = order.OrderQty.Value;
                //Procesamientos especiales de la cantidad de las ordenes
                if (order.Security.SecType == zHFT.Main.Common.Enums.SecurityType.TB)
                {
                    orderQty *= 1000;
                }

                QuickFix.Message cancelMessage = FIXMessageCreator.CreateOrderCancelRequest(
                                                newMarketOrderIdRequested.ToString(),
                                                marketOrderId.ToString(),
                                                order.OrderId,
                                                order.Security.Symbol, order.Side,
                                                order.EffectiveTime.Value,
                                                orderQty, order.Account,
                                                _MAIN_EXCHANGE
                                                );



                //Session.sendToTarget(cancelMessage, SessionID);
                Thread cancelThread = new Thread(DoCancel);
                cancelThread.Start(cancelMessage);
            }
            else
            {
                DoLog(string.Format("@{0}:Order for ClOrdId {1} not found! @ActiveOrderIdMapper ", GetConfig().Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
                throw new Exception(string.Format("@{0}:Order for ClOrdId {1} not found!!  @ActiveOrderIdMapper", GetConfig().Name, clOrdId));

            }
        
        }

        protected CMState CancelOrder(Wrapper wrapper)
        {
            try
            {
                if (SessionID != null)
                {
                    string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);
                    DoLog(string.Format("@{0}:Cancelling all orders @Primary ", GetConfig().Name), Main.Common.Util.Constants.MessageType.Information);

                    if (ActiveOrders.Keys.Contains(clOrdId))
                    {
                        Order order = ActiveOrders[clOrdId];

                        ExecCancel(order);

                        return CMState.BuildSuccess();

                    }
                    else
                    {
                        DoLog(string.Format("@{0}:Order for ClOrdId {1} not found! @ActiveOrders ", GetConfig().Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
                        throw new Exception(string.Format("@{0}:Order for ClOrdId {1} not found!!  @ActiveOrders", GetConfig().Name, clOrdId));
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Session not initialized on cancel order ", GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildSuccess();
                }

            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }

        }

        #endregion

        #region Public Methods

        public void DoLog(string msg, Main.Common.Util.Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        #endregion
    }
}

using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
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

namespace zHFT.BasedFullMarketConnectivity.Primary.Common
{
    public abstract class BaseFullMarketConnectivity : Application
    {
        #region Private  Consts

        protected int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        protected int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        protected string _DUMMY_SECURITY = "kcdlsncslkd";

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

        protected Dictionary<string, int> ActiveOrderIdMapper { get; set; }

        protected Dictionary<string, int> ReplacingActiveOrderIdMapper { get; set; }

        #endregion

        #region abstract Methods

        public abstract BaseConfiguration GetConfig();

        protected abstract void ProcessSecurities(SecurityList securityList);

        #endregion

        #region abstract QuickFix Methods
        public abstract void fromApp(Message value, SessionID sessioId);
        public abstract void toAdmin(Message value, SessionID sessioId);
        #endregion

        #region Quickfix Methods

        public virtual void fromAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de fromAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public virtual void onCreate(SessionID value)
        {
            lock (tLock)
            {
                DoLog("Invocación de onCreate : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public virtual void onLogon(SessionID value)
        {
            lock (tLock)
            {
                SessionID = value;
                DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

                if (SessionID != null)
                    DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);
                else
                    DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);
            }
        }

        public virtual void onLogout(SessionID value)
        {
            lock (tLock)
            {
                SessionID = null;
                DoLog("Invocación de onLogout : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public virtual void toApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de toApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        #endregion

        #region Protected Methods

        protected ExecutionReportWrapper ProcesssExecutionReportMessage(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", GetConfig().Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            ExecutionReportWrapper erWrapper = new ExecutionReportWrapper((QuickFix50.ExecutionReport)message, GetConfig());

            string marketClOrdId = (string)erWrapper.GetField(ExecutionReportFields.ClOrdID);
            string marketOrigClOrdId = (string)erWrapper.GetField(ExecutionReportFields.OrigClOrdID);
            zHFT.Main.Common.Enums.ExecType execType = (zHFT.Main.Common.Enums.ExecType)erWrapper.GetField(ExecutionReportFields.ExecType);

            string clOrdId = marketClOrdId != null ? ActiveOrderIdMapper.Keys.Where(x => ActiveOrderIdMapper[x].ToString() == marketClOrdId).FirstOrDefault() : null;
            string origClOrdId = marketOrigClOrdId != null ? ReplacingActiveOrderIdMapper.Keys.Where(x => ReplacingActiveOrderIdMapper[x].ToString() == marketOrigClOrdId).FirstOrDefault() : null;

            if (clOrdId != null)
            {
                if (ActiveOrders.Keys.Contains(clOrdId))
                {
                    if (execType == zHFT.Main.Common.Enums.ExecType.New)
                    {
                        Order order = ActiveOrders[clOrdId];
                        string orderId = (string)erWrapper.GetField(ExecutionReportFields.OrderID);
                        order.OrderId = orderId;
                    }
                }
                else if (!string.IsNullOrEmpty(origClOrdId) && ActiveOrders.Keys.Contains(origClOrdId))
                {
                    if (execType == zHFT.Main.Common.Enums.ExecType.Replaced)
                    {
                        Order order = ActiveOrders[origClOrdId];
                        string orderId = (string)erWrapper.GetField(ExecutionReportFields.OrderID);
                        order.OrderId = orderId;

                        ActiveOrders.Add(clOrdId, order);
                        ReplacingActiveOrderIdMapper.Remove(origClOrdId);
                        ActiveOrderIdMapper.Add(clOrdId, Convert.ToInt32(marketClOrdId));
                    }
                }
                else
                {
                    DoLog(string.Format("@{0} Could not find order for ClOrderId {1} ", GetConfig().Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
                }

            }
            else
            {
                DoLog(string.Format("@{0} Could not find ClOrderId for Execution Report!", GetConfig().Name), Main.Common.Util.Constants.MessageType.Error);
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
                        Session.sendToTarget(rq, SessionID);
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

        protected CMState ProcessMarketDataRequest(Wrapper marketDataRequestWrapper)
        {
            if (SessionID != null)
            {
                MarketDataRequest rq = MarketDataRequestConverter.GetMarketDataRequest(marketDataRequestWrapper);

                QuickFix.Message msg = FIXMessageCreator.RequestMarketData(MarketDataRequestId, rq.Security.Symbol, rq.SubscriptionRequestType);
                MarketDataRequestId++;

                Session.sendToTarget(msg, SessionID);

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
            try
            {
                if (SessionID != null)
                {
                    lock (tLock)
                    {
                        Order newOrder = OrderConverter.ConvertNewOrder(wrapper);
                        newOrder.EffectiveTime = DateTime.Now;
                        int marketClOrdId = (OrderIndexId * 100);

                        OrderIndexId++;

                        QuickFix.Message msg = FIXMessageCreator.CreateNewOrderSingle(marketClOrdId.ToString(), 
                                                                                      newOrder.Symbol, 
                                                                                      newOrder.Side, 
                                                                                      newOrder.OrdType,
                                                                                      newOrder.SettlType, 
                                                                                      newOrder.TimeInForce, 
                                                                                      newOrder.EffectiveTime.Value,
                                                                                      newOrder.OrderQty.Value, 
                                                                                      newOrder.Price,
                                                                                      newOrder.StopPx, 
                                                                                      newOrder.Account);

                        Session.sendToTarget(msg, SessionID);

                        ActiveOrders.Add(newOrder.ClOrdId, newOrder);
                        ActiveOrderIdMapper.Add(newOrder.ClOrdId,marketClOrdId);
                    }

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
                return CMState.BuildFail(ex);
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
                        int marketOrderId = ActiveOrderIdMapper[origClOrdId];

                        int newMarketOrderIdRequested = (marketOrderId + 1);
                        ReplacingActiveOrderIdMapper.Add(origClOrdId, newMarketOrderIdRequested);

                        order.ClOrdId = clOrdId;
                        order.OrigClOrdId = origClOrdId;

                        QuickFix.Message cancelMessage = FIXMessageCreator.CreateOrderCancelReplaceRequest(
                                                                            newMarketOrderIdRequested.ToString(),
                                                                            order.OrderId,
                                                                            marketOrderId.ToString(),
                                                                            order.Security.Symbol,
                                                                            order.Side,
                                                                            order.OrdType,
                                                                            order.SettlType,
                                                                            order.TimeInForce,
                                                                            ordQty,//qty to update
                                                                            price,//price to update
                                                                            order.StopPx,
                                                                            order.Account
                                                                            );

                        Session.sendToTarget(cancelMessage, SessionID);

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

        protected CMState CancelOrder(Wrapper wrapper)
        {
            try
            {
                if (SessionID != null)
                {
                    string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);

                    if (ActiveOrders.Keys.Contains(clOrdId))
                    {
                        Order order = ActiveOrders[clOrdId];

                        int marketOrderId = ActiveOrderIdMapper[clOrdId];

                        int newMarketOrderIdRequested = (marketOrderId + 1);
                        ReplacingActiveOrderIdMapper.Add(clOrdId, newMarketOrderIdRequested);

                        order.ClOrdId = (Convert.ToInt32(clOrdId) + 1).ToString();
                        order.OrigClOrdId = clOrdId;

                        QuickFix.Message cancelMessage = FIXMessageCreator.CreateOrderCancelRequest(
                                                        newMarketOrderIdRequested.ToString(),
                                                        marketOrderId.ToString(),
                                                        order.OrderId,
                                                        order.Security.Symbol, order.Side,
                                                        order.EffectiveTime.Value,
                                                        order.OrderQty, order.Account
                                                        );

                        Session.sendToTarget(cancelMessage, SessionID);

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

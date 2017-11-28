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

        protected CMState RouteNewOrder(Wrapper wrapper)
        {
            try
            {
                if (SessionID != null)
                {
                    lock (tLock)
                    {
                        Order newOrder = OrderConverter.ConvertNewOrder(wrapper);

                        newOrder.ClOrdId = (OrderIndexId * 100).ToString();
                        OrderIndexId++;

                        QuickFix.Message msg = FIXMessageCreator.CreateNewOrderSingle(newOrder.ClOrdId, newOrder.Symbol, newOrder.Side, newOrder.OrdType,
                                                                                      newOrder.SettlType, newOrder.TimeInForce, newOrder.OrderQty.Value, newOrder.Price,
                                                                                      newOrder.StopPx, newOrder.Account);

                        Session.sendToTarget(msg, SessionID);

                        ActiveOrders.Add(newOrder.ClOrdId, newOrder);
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
                    double? ordQty = (double?)wrapper.GetField(OrderFields.OrderQty);
                    double? price = (double?)wrapper.GetField(OrderFields.Price);

                    if (ActiveOrders.Keys.Contains(clOrdId))
                    {
                        Order order = ActiveOrders[clOrdId];

                        string newClOrderIdRequested = (Convert.ToInt32(clOrdId) + 1).ToString();



                        QuickFix.Message cancelMessage = FIXMessageCreator.CreateOrderCancelReplaceRequest(
                                                                            newClOrderIdRequested,
                                                                            order.OrderId,
                                                                            order.ClOrdId,
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

                        order.ClOrdId = (Convert.ToInt32(clOrdId) + 1).ToString();
                        order.OrigClOrdId = clOrdId;


                        QuickFix.Message cancelMessage = FIXMessageCreator.CreateOrderCancelRequest(
                                                        order.ClOrdId,
                                                        null,
                                                        order.OrderId,
                                                        order.Security.Symbol, order.Side,
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

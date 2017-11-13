using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;
using zHFT.MarketClient.Primary.Common.Wrappers;
using zHFT.MessageBasedFullMarketConnectivity.Primary.Common.Converters;
using zHFT.OrderRouters.Primary.Common.Wrappers;
using zHFT.SecurityListMarketClient.Primary.Common.Converters;
using zHFT.SingletonModulesHandler.Common.Interfaces;
using zHFT.SingletonModulesHandler.Common.Util;

namespace zHFT.MessageBasedFullMarketConnectivity.Primary
{
    public class MessageBasedMarketClient : Application, ISingletonModule
    {

        #region Constructors

        private MessageBasedMarketClient(OnLogMessage pOnLogMsg, string configFile)
        {
            Initialize(pOnLogMsg, configFile);
        }

        #endregion

        #region Private  Consts

        private string _DUMMY_SECURITY = "kcdlsncslkd";

        #endregion

        #region Private Static Attributes

        private static MessageBasedMarketClient Instance { get; set; }

        #endregion

        #region Public Static Methods

        public static MessageBasedMarketClient GetInstance(OnLogMessage pOnLogMsg, string configFile)
        {
            if (Instance == null)
            {
                Instance = new MessageBasedMarketClient(pOnLogMsg, configFile);
            }
            return Instance;
        }

        #endregion

        #region Protected Attributes

        protected IConfiguration Config { get; set; }

        protected Common.Configuration.Configuration PrimaryConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        private IFIXMessageCreator FIXMessageCreator { get; set; }

        protected SessionSettings SessionSettings { get; set; }
        protected FileStoreFactory FileStoreFactory { get; set; }
        protected ScreenLogFactory ScreenLogFactory { get; set; }
        protected SessionID SessionID { get; set; }
        protected MessageFactory MessageFactory { get; set; }
        protected SocketInitiator Initiator { get; set; }

        protected OnMessageReceived OnIncomingMessageRcv { get; set; }

        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected object tLock = new object();

        protected int MarketDataRequestId { get; set; }

        protected int OrderIndexId { get; set; }

        protected OrderConverter OrderConverter { get; set; }

        protected Dictionary<string, Order> ActiveOrders { get; set; }

        #endregion

        #region Protected Methods

        protected CMState ProcessMarketDataRequest(Wrapper marketDataRequestWrapper)
        {
            if (SessionID != null)
            {
                string exchange = (string)marketDataRequestWrapper.GetField(MarketDataRequestField.Exchange);
                string fullSymbol = (string)marketDataRequestWrapper.GetField(MarketDataRequestField.Symbol);
                string exchangePrefixCode = ExchangeConverter.GetMarketPrefixCode(exchange);
                zHFT.Main.Common.Enums.SecurityType secType = (zHFT.Main.Common.Enums.SecurityType)marketDataRequestWrapper.GetField(MarketDataRequestField.SecurityType);
                string marketClearingID = ExchangeConverter.GetMarketClearingID(secType, exchange);


                MarketDataRequest rq = MarketDataRequestConverter.GetMarketDataRequest(marketDataRequestWrapper, exchangePrefixCode,
                                                                                        marketClearingID, secType);

                QuickFix.Message msg = FIXMessageCreator.RequestMarketData(MarketDataRequestId, rq.Symbol);
                MarketDataRequestId++;

                Session.sendToTarget(msg, SessionID);

                return CMState.BuildSuccess();
              
            }
            else
            {
                DoLog(string.Format("@{0}:Session not initialized on new market data request ", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildSuccess();
            }
        }

        protected void ProcesssMDFullRefreshMessage(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", PrimaryConfiguration.Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            string primarySymbol = message.getField(Symbol.FIELD);

            string market = ExchangeConverter.GetMarketFromFullSymbol(primarySymbol);
            string fullSymbol = SecurityConverter.GetFullSymbolFromPrimary(primarySymbol, market);

            Security sec = new Security()
            {
                Symbol = SecurityConverter.GetCleanSymbolFromPrimary(primarySymbol, market),
                Exchange = market
            };

            FIXMessageCreator.ProcessMarketData(message, sec, OnLogMsg);

            MarketDataWrapper mdWrapper = new zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper(sec, market, PrimaryConfiguration);

            OnIncomingMessageRcv(mdWrapper);
        
        }

        protected void ProcesssExecutionReportMessage(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", PrimaryConfiguration.Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            ExecutionReportWrapper erWrapper = new ExecutionReportWrapper((QuickFix50.ExecutionReport )message, Config);

            string clOrdId = (string) erWrapper.GetField(ExecutionReportFields.ClOrdID);
            string origClOrdId = (string)erWrapper.GetField(ExecutionReportFields.OrigClOrdID);
            zHFT.Main.Common.Enums.ExecType execType = (zHFT.Main.Common.Enums.ExecType)erWrapper.GetField(ExecutionReportFields.ExecType);

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
                    }
                }
                else
                {
                    DoLog(string.Format("@{0} Could not find order for ClOrderId {1} ", PrimaryConfiguration.Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
                }

            }
            else
            {
                DoLog(string.Format("@{0} Could not find ClOrderId for Execution Report!", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
            }

            OnIncomingMessageRcv(erWrapper);
        }

        protected void ProcesssBusinessMessageReject(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", PrimaryConfiguration.Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            BusinessMessageRejectWrapper bmrWrapper = new BusinessMessageRejectWrapper((QuickFix50.BusinessMessageReject)message);

            OnIncomingMessageRcv(bmrWrapper);
        
        }

        protected CMState ProccessBusinessMessageReject(Wrapper wrapper)
        {
            string refMsgType = (string)wrapper.GetField(BusinessMessageRejectField.RefMsgType);
            string businessRejectRefID = (string)wrapper.GetField(BusinessMessageRejectField.BusinessRejectRefID);
            zHFT.Main.Common.Enums.BusinessRejectReason businessRejectReason = (zHFT.Main.Common.Enums.BusinessRejectReason)wrapper.GetField(BusinessMessageRejectField.BusinessRejectReason);


            DoLog(string.Format("@{0}:Processing business message reject for refMsgType {1} - businessRejectRefID {2} - BusinessRejectReason {3} ",
                                PrimaryConfiguration.Name, refMsgType, businessRejectRefID, businessRejectReason.ToString()),
                                Main.Common.Util.Constants.MessageType.Information);



            return CMState.BuildSuccess();
        }

        protected void ProcesssOrderCancelReject(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", PrimaryConfiguration.Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            OrderCancelRejectWrapper ocrWrapper = new OrderCancelRejectWrapper((QuickFix50.OrderCancelReject)message);

            OnIncomingMessageRcv(ocrWrapper);
        }

        protected CMState ProccessOrderCancelRejectMessage(Wrapper wrapper)
        {
            string clOrdId = (string)wrapper.GetField(OrderCancelRejectField.ClOrdID);
            string orderId = (string)wrapper.GetField(OrderCancelRejectField.OrderID);
            string text = (string)wrapper.GetField(OrderCancelRejectField.Text);

            DoLog(string.Format("@{0}:Processing order cancel reject for ClOrdId {1} - OrderId {2}:{3} ",
                                PrimaryConfiguration.Name, clOrdId, orderId, text),
                                Main.Common.Util.Constants.MessageType.Information);



            return CMState.BuildSuccess();
        }

        protected CMState ProcessExecutionReportMessage(Wrapper wrapper)
        {
            string symbol = (string) wrapper.GetField(ExecutionReportFields.Symbol);
            string origClOrdId = (string)wrapper.GetField(ExecutionReportFields.OrigClOrdID);
            string clOrdId = (string)wrapper.GetField(ExecutionReportFields.ClOrdID);
            string orderId = (string)wrapper.GetField(ExecutionReportFields.OrderID);
            string text= (string)wrapper.GetField(ExecutionReportFields.Text);

            zHFT.Main.Common.Enums.OrdStatus status = (zHFT.Main.Common.Enums.OrdStatus)wrapper.GetField(ExecutionReportFields.OrdStatus);
            zHFT.Main.Common.Enums.ExecType execType = (zHFT.Main.Common.Enums.ExecType)wrapper.GetField(ExecutionReportFields.ExecType);

            #region DoLog
            if (!ActiveOrders.Keys.Contains(clOrdId) && string.IsNullOrEmpty(origClOrdId))
            {
                DoLog(string.Format("@{0} Could not find order  execution report for OrigClOrdId={1} ClOrdId={2} Status={3} ",
                                   PrimaryConfiguration.Name, origClOrdId, clOrdId, status.ToString()),
                                   Main.Common.Util.Constants.MessageType.Error);

                return CMState.BuildSuccess();
            }
            else if (!string.IsNullOrEmpty(origClOrdId) && !ActiveOrders.Keys.Contains(origClOrdId))
            {
                DoLog(string.Format("@{0} Could not find order  execution report for OrigClOrdId={1} ClOrdId={2} Status={3} ",
                                   PrimaryConfiguration.Name, origClOrdId, clOrdId, status.ToString()),
                                   Main.Common.Util.Constants.MessageType.Error);

                return CMState.BuildSuccess();
            }
            else
            {
                DoLog(string.Format("@{0} Processing execution report for OrigClOrdId={1} ClOrdId={2} Status={3} ",
                                       PrimaryConfiguration.Name, origClOrdId, clOrdId, status.ToString()),
                                       Main.Common.Util.Constants.MessageType.Information);
            }
            #endregion

            if (execType == zHFT.Main.Common.Enums.ExecType.Canceled)
            {
                if (ActiveOrders.Keys.Contains(origClOrdId))
                {
                    Order order = ActiveOrders[origClOrdId];
                    order.ClOrdId = clOrdId;
                    order.OrdStatus = status;
                }
            }
            else if (execType == zHFT.Main.Common.Enums.ExecType.Replaced)
            {//Tengo que actualizar el Id 
                if (ActiveOrders.Keys.Contains(origClOrdId))
                {
                    Order rplOrder = ActiveOrders[origClOrdId];

                    double? newOrdQty = (double)wrapper.GetField(ExecutionReportFields.OrderQty);
                    double? newPrice = (double)wrapper.GetField(ExecutionReportFields.Price);

                    rplOrder.ClOrdId = clOrdId;
                    rplOrder.OrigClOrdId = origClOrdId;
                    rplOrder.OrdStatus = status;
                    rplOrder.OrderQty = newOrdQty;
                    rplOrder.Price = newPrice;

                    ActiveOrders.Remove(origClOrdId);
                    ActiveOrders.Add(clOrdId, rplOrder);
                }
            }
            else
            {

                if (ActiveOrders.Keys.Contains(clOrdId))
                {
                    Order order = ActiveOrders[clOrdId];
                    order.OrdStatus = status;
                }
            }

            return CMState.BuildSuccess();
        }

        protected CMState ProcessSecurityListRequest(Wrapper wrapper)
        {
            if (SessionID != null)
            {
                zHFT.Main.Common.Enums.SecurityListRequestType type = (zHFT.Main.Common.Enums.SecurityListRequestType)wrapper.GetField(SecurityListRequestField.SecurityListRequestType);

                if (type == zHFT.Main.Common.Enums.SecurityListRequestType.AllSecurities)
                {
                    QuickFix.Message rq = FIXMessageCreator.RequestSecurityList((int)type, _DUMMY_SECURITY);
                    Session.sendToTarget(rq, SessionID);
                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0} SecurityListRequestType not implemented: {1}", PrimaryConfiguration.Name, type.ToString()));
            }
            else
            {
                DoLog(string.Format("@{0}:Session not initialized on security list request ", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildSuccess();
            }
        }

        protected CMState RouteNewOrder(Wrapper wrapper)
        {
            try
            {
                if(SessionID!=null)
                {
                    lock(tLock)
                    {
                        Order newOrder = OrderConverter.ConvertNewOrder(wrapper);

                        newOrder.ClOrdId=(OrderIndexId*100).ToString();
                        OrderIndexId++;

                        QuickFix.Message msg = FIXMessageCreator.CreateNewOrderSingle(newOrder.ClOrdId,newOrder.Symbol,newOrder.Side,newOrder.OrdType,
                                                                                      newOrder.SettlType,newOrder.TimeInForce,newOrder.OrderQty.Value,newOrder.Price,
                                                                                      newOrder.StopPx,newOrder.Account);
                    
                        Session.sendToTarget(msg,SessionID);

                        ActiveOrders.Add(newOrder.ClOrdId, newOrder);
                    }
                    
                    return CMState.BuildSuccess();
                }
                else
                {
                    DoLog(string.Format("@{0}:Session not initialized on new order ", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildSuccess();
                }

            }
            catch(Exception ex)
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
                        DoLog(string.Format("@{0}:Order for ClOrdId {1} not found! ", PrimaryConfiguration.Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
                        throw new Exception(string.Format("@{0}:Order for ClOrdId {1} not found!!", PrimaryConfiguration.Name, clOrdId));
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Session not initialized on update order ", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildSuccess();
                }

            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }

        }

        protected CMState DisplayFullOrderList(Wrapper wrapper)
        {
            string result = "----------------------- ORDER LIST -----------------------" + System.Environment.NewLine;


            foreach (Order order in ActiveOrders.Values)
            {
                result += string.Format("ClOrdId={0} OrderId={1} Symbol={2} Exchange={3} Side={4} Qty={5} Price={6} OrdStatus={7} {8}",
                                       order.ClOrdId, order.OrderId, order.Symbol, order.Exchange, order.Side.ToString(),
                                       order.OrderQty.HasValue ? order.OrderQty.Value.ToString("0.##") : "",
                                       order.Price.HasValue ? order.Price.Value.ToString("0.##") : "",
                                       order.OrdStatus.ToString(), System.Environment.NewLine);
            }

            result += "----------------------- ORDER LIST END  -----------------------" + System.Environment.NewLine;

            DoLog(result, Main.Common.Util.Constants.MessageType.Error);

            return CMState.BuildSuccess();
        }

        protected CMState CancelOrder(Wrapper wrapper)
        {
            try
            {
                if (SessionID != null)
                {
                    string clOrdId = (string) wrapper.GetField(OrderFields.ClOrdID);

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
                        DoLog(string.Format("@{0}:Order for ClOrdId {1} not found! ", PrimaryConfiguration.Name, clOrdId), Main.Common.Util.Constants.MessageType.Error);
                        throw new Exception(string.Format("@{0}:Order for ClOrdId {1} not found!!", PrimaryConfiguration.Name, clOrdId));
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Session not initialized on cancel order ", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
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

        public bool Initialize(OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnLogMsg += pOnLogMsg;

                if (ConfigLoader.DoLoadConfig(this, configFile))
                {
                    MarketDataRequestId = 1;
                    OrderIndexId = 1;
                    OrderConverter = new OrderConverter();
                    ActiveOrders = new Dictionary<string, Order>();

                    var fixMessageCreator = Type.GetType(PrimaryConfiguration.FIXMessageCreator);
                    if (fixMessageCreator != null)
                    {
                        FIXMessageCreator = (IFIXMessageCreator)Activator.CreateInstance(fixMessageCreator);
                    }
                    else
                        throw new Exception(string.Format("@{0}:Assembly not found: " + PrimaryConfiguration.FIXMessageCreator));


                    SessionSettings = new SessionSettings(PrimaryConfiguration.FIXInitiatorPath);
                    FileStoreFactory = new FileStoreFactory(SessionSettings);
                    ScreenLogFactory = new ScreenLogFactory(SessionSettings);
                    MessageFactory = new DefaultMessageFactory();

                    Initiator = new SocketInitiator(this, FileStoreFactory, SessionSettings, ScreenLogFactory, MessageFactory);

                    Initiator.start();

                    return true;

                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing {1}:{2}", PrimaryConfiguration.Name,
                                                                              configFile,
                                                                              ex.Message),
                                                                              Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        public CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (action == Actions.SECURITY_LIST_REQUEST)
                    {
                        DoLog(string.Format("Receiving Security List Request: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);

                        return ProcessSecurityListRequest(wrapper);
                    }
                    else if (action == Actions.MARKET_DATA_REQUEST)
                    {
                        DoLog(string.Format("Receiving Market Data Request: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return ProcessMarketDataRequest(wrapper);
                    }
                    else if (action == Actions.MARKET_DATA)
                    {
                        DoLog(string.Format("Receiving Market Data: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildSuccess();
                    }
                    else if (action == Actions.EXECUTION_REPORT)
                    {
                        DoLog(string.Format("Receiving Execution Report from other module: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return ProcessExecutionReportMessage(wrapper);
                    }
                    else if (action == Actions.ORDER_CANCEL_REJECT)
                    {
                        DoLog(string.Format("Receiving Order CancelReject from other module: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return ProccessOrderCancelRejectMessage(wrapper);
                    }
                    else if (action == Actions.BUSINESS_MESSAGE_REJECT)
                    {
                        DoLog(string.Format("Receiving Business Message Reject from other module: {0}", wrapper.ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return ProccessBusinessMessageReject(wrapper);
                    }
                    else if (wrapper.GetAction() == Actions.NEW_ORDER)
                    {
                        DoLog(string.Format("@{0}:Routing with Primary to market for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        return RouteNewOrder(wrapper);
                    }
                    else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                    {
                        DoLog(string.Format("@{0}:Updating order with Primary  for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        UpdateOrder(wrapper);
                        return CMState.BuildSuccess();

                    }
                    else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                    {
                        DoLog(string.Format("@{0}:Canceling order with Primary  for ClOrdId {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.ClOrdID).ToString()), Main.Common.Util.Constants.MessageType.Information);
                        CancelOrder(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (wrapper.GetAction() == Actions.ORDER_LIST)
                    {
                        DoLog(string.Format("@{0}:Displaying full order list active in memory", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        DisplayFullOrderList(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else
                    {
                        DoLog(string.Format("@{0}:Sending message " + action + " not implemented", PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message " + action + " not implemented", PrimaryConfiguration.Name)));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");

            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion

        #region ISingletonModule Methods

        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);

        }

        public void DoLog(string msg, Main.Common.Util.Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        public void SetOutgoingEvent(Main.Common.Interfaces.OnMessageReceived OnMessageRcv)
        {
            OnExecutionReportMessageRcv += OnMessageRcv;
        }

        public void SetIncomingEvent(Main.Common.Interfaces.OnMessageReceived OnMessageRcv)
        {
            OnIncomingMessageRcv += OnMessageRcv;
        }

        #endregion

        #region QuickFix Methods

        public void fromAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de fromAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void fromApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                try
                {
                    DoLog("Invocación de fromApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

                    if (value is QuickFix50.MarketDataIncrementalRefresh)
                    {
                        DoLog(string.Format("{0}: Market Data Incremental Refresh Message received and not processed:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Error);

                    }
                    else if (value is QuickFix50.MarketDataSnapshotFullRefresh)
                    {
                        QuickFix50.MarketDataSnapshotFullRefresh msg = (QuickFix50.MarketDataSnapshotFullRefresh)value;
                        ProcesssMDFullRefreshMessage(msg);

                    }
                    else if (value is QuickFix50.ExecutionReport)
                    {
                        QuickFix50.ExecutionReport msg = (QuickFix50.ExecutionReport)value;
                        ProcesssExecutionReportMessage(msg);
                    }
                    else if (value is QuickFix50.OrderCancelReject)
                    {
                        QuickFix50.OrderCancelReject msg = (QuickFix50.OrderCancelReject)value;
                        ProcesssOrderCancelReject(msg);
                    }
                    else if (value is QuickFix50.BusinessMessageReject)
                    {
                        QuickFix50.BusinessMessageReject msg = (QuickFix50.BusinessMessageReject)value;
                        ProcesssBusinessMessageReject(msg);
                    }
                    else if (value is QuickFix50Sp2.MarketDataRequestReject)
                    {
                        DoLog(string.Format("{0}: MarketDataRequestReject:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Error);
                    }
                    else
                    {
                        DoLog(string.Format("{0}: Unknown message:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Information);
                    }
                }
                catch (Exception ex)
                {

                    DoLog(string.Format("{0}: Error processing message @fromApp:{1} ", PrimaryConfiguration.Name, ex.Message), Constants.MessageType.Error);

                }
            }
        }

        public void onCreate(SessionID value)
        {
            lock (tLock)
            {
                DoLog("Invocación de onCreate : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void onLogon(SessionID value)
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

        public void onLogout(SessionID value)
        {
            lock (tLock)
            {
                SessionID = null;
                DoLog("Invocación de onLogout : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void toAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                try
                {
                    if (value is QuickFixT11.Logon)
                    {
                        QuickFixT11.Logon logon = (QuickFixT11.Logon)value;
                        logon.setField(Username.FIELD, PrimaryConfiguration.User);
                        logon.setField(Password.FIELD, PrimaryConfiguration.Password);
                        DoLog("Invocación de toAdmin-logon por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                    }
                    else if (value is QuickFixT11.Reject)
                    {
                        QuickFixT11.Reject reject = (QuickFixT11.Reject)value;
                        DoLog("Invocación de toAdmin-reject por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                    }
                    else
                        DoLog("Invocación de toAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("{0}: Error processing message @toAdmin:{1} ", PrimaryConfiguration.Name, ex.Message), Constants.MessageType.Error);
                }
            }
        }

        public void toApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de toApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        #endregion
    }
}

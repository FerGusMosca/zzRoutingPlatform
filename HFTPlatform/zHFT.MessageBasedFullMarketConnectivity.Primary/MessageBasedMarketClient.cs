using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.BasedFullMarketConnectivity.Primary.Common;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;
using zHFT.MarketClient.Primary.Common.Wrappers;
using zHFT.OrderRouters.Primary.Common;
using zHFT.OrderRouters.Primary.Common.Wrappers;
using zHFT.SingletonModulesHandler.Common.Interfaces;
using zHFT.SingletonModulesHandler.Common.Util;

namespace zHFT.MessageBasedFullMarketConnectivity.Primary
{
    public class MessageBasedMarketClient : BaseFullMarketConnectivity, ISingletonModule
    {

        #region Constructors

        private MessageBasedMarketClient(OnLogMessage pOnLogMsg, string configFile)
        {
            Initialize(pOnLogMsg, configFile);
        }

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

        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        protected OnMessageReceived OnIncomingMessageRcv { get; set; }

        #endregion

        #region Protected Methods

        protected void ProcesssMDFullRefreshMessage(QuickFix.Message message)
        {
            DoLog(string.Format("@{0}:{1} ", PrimaryConfiguration.Name, message.ToString()), Main.Common.Util.Constants.MessageType.Information);

            string primarySymbol = message.getField(Symbol.FIELD);

            string market = ExchangeConverter.GetMarketFromPrimarySymbol(primarySymbol);
            string fullSymbol = SymbolConverter.GetFullSymbolFromPrimary(primarySymbol, market);

            Security sec = new Security()
            {
                Symbol = SymbolConverter.GetCleanSymbolFromPrimary(primarySymbol, market),
                Exchange = market
            };

            FIXMessageCreator.ProcessMarketData(message, sec, OnLogMsg);

            MarketDataWrapper mdWrapper = new zHFT.MarketClient.Primary.Common.Wrappers.MarketDataWrapper(sec, market, PrimaryConfiguration);

            OnIncomingMessageRcv(mdWrapper);
        
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

        protected override void ProcessSecurities(Main.BusinessEntities.Security_List.SecurityList securityList)
        {
           //No procesamos security lists
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
                    OrderIndexId = GetNextOrderId();
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

        public override BaseConfiguration GetConfig() { return (BaseConfiguration)PrimaryConfiguration; } 

        #endregion

        #region ISingletonModule Methods

        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);

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

        public override void fromApp(Message value, SessionID sessionId)
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
                        ExecutionReportWrapper erWrapper = ProcesssExecutionReportMessage(msg);
                        OnExecutionReportMessageRcv(erWrapper);
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

        public override void toAdmin(Message value, SessionID sessionId)
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

        #endregion

        protected override void CancelMarketData(Security sec)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper;
using tph.StrategyHandler.SimpleCommandSender.Common.Configuration;
using tph.StrategyHandler.SimpleCommandSender.Common.Wrappers;
using tph.StrategyHandler.SimpleCommandSender.ServiceLayer;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.OrderRouters.Common.Converters;
using zHFT.OrderRouters.Common.Wrappers;
using zHFT.SingletonModulesHandler.Common.Interfaces;

namespace tph.StrategyHandler.SimpleCommandSender
{
    public class OrderRouterWSClient: BaseCommunicationModule, ILogger,ISingletonModule
    {
        
        #region Protected Attributs
        
        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        protected OnMessageReceived OnIncomingMessageRcv { get; set; }
        
        protected  static ISingletonModule Instance { get; set; }
        
        protected WebSocketClient WebSocketClient { get; set; }
        
        public tph.StrategyHandler.SimpleCommandSender.Common.Configuration.Configuration Config { get; set; }
        
        protected Dictionary<string,long> MarketDataRequests { get; set; }
        
        protected Dictionary<string,long> CandlebarRequests { get; set; }
        
        protected  Dictionary<string,NewOrderReq> JsonOrdersDict { get; set; }
        
        protected OrderConverter WrapperOrderConverter { get; set; }
        
        protected tph.StrategyHandler.SimpleCommandSender.Common.Util.OrderConverter JsonOrderConverter { get; set; }
        
        #endregion

        #region Constructors


        public OrderRouterWSClient(OnLogMessage pOnLogMsg, string configFile)
        {
            Initialize(null,pOnLogMsg, configFile);

        }


        #endregion

        #region Public Static Methods

        public static ISingletonModule GetInstance(OnLogMessage pOnLogMsg, string configFile)
        {
            if (Instance == null)
            {
                Instance = new OrderRouterWSClient(pOnLogMsg,configFile);
            }
            return Instance;
        }

        #endregion

        #region Websocket Methods
        
        public  void ProcessEvent(WebSocketMessage msg)
        {

            if (msg.Msg == "AuthenticationResp")
            {
                //TODO process AuthResp

            }
            else if (msg.Msg == "ClientDepthOfBook")
            {
                //TODO process ClientDepthOfBook

            }

            DoLog($"{Config.Name}--> Recv msg:{msg.ToString()}", Constants.MessageType.Information);
        }

        public void ProcessIncomingAsync(object param)
        {
            try
            {
                Wrapper mdWrapper = (Wrapper) param;
                OnIncomingMessageRcv(mdWrapper);
            }
            catch (Exception e)
            {
               DoLog($"ERROR @ProcessIncomingAsync:{e.Message}",Constants.MessageType.Error);
            }
        }
        
        public void ProcessExecutionReportAsync(object param)
        {
            try
            {
                Wrapper erWrapper = (Wrapper) param;
                OnExecutionReportMessageRcv(erWrapper);
            }
            catch (Exception e)
            {
                DoLog($"ERROR @ProcessExecutionReportAsync:{e.Message}",Constants.MessageType.Error);
            }
        }

        public void DoSendAsync<T>(object param)
        {
            try
            {
                T objToSend = (T) param;
                string strReq = JsonConvert.SerializeObject(objToSend, Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                WebSocketClient.Send(strReq);
                
            }
            catch (Exception e)
            {
                DoLog($"ERROR @DoSendAsync:{e.Message}",Constants.MessageType.Error);
            }
            
        }

        public void ProcessCandlebar(CandlebarDTO msg)
        {
            
            try
            {
                lock (MarketDataRequests)
                {
                    if (MarketDataRequests.ContainsKey(msg.Symbol))
                    {
                        long mdReqId = CandlebarRequests[msg.Symbol];
                        Security sec = new Security();
                        sec.Symbol = msg.Symbol;
                        //sec.MarketData = msg;
                        throw new Exception($"Candlebar wrapper not implemented @ProcessCandlebar");
                        //All updates are Snapshot and updates in this module

                        MarketDataWrapper mdWrapper = new MarketDataWrapper(sec, Config);
                        (new Thread(ProcessIncomingAsync)).Start(mdWrapper);

                        DoLog($"{Config.Name}--> Recv Market Data:{msg.ToString()}", Constants.MessageType.Information);
                    }
                    else
                    {
                        DoLog($"Ignoring candlebar for unknown symbol {msg.Symbol}", Constants.MessageType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"ERROR processing candlebar {ex.Message}",Constants.MessageType.Information);
            }
        }

        public  void ProcessMarketData(MarketDataDTO msg)
        {
            try
            {
                lock (MarketDataRequests)
                {
                    if (MarketDataRequests.ContainsKey(msg.Symbol))
                    {
                        long mdReqId = MarketDataRequests[msg.Symbol];
                        Security sec = new Security();
                        sec.Symbol = msg.Symbol;
                        sec.MarketData = msg;

                        //All updates are Snapshot and updates in this module
                        MarketDataWrapper mdWrapper = new MarketDataWrapper(sec, Config);

                        (new Thread(ProcessIncomingAsync)).Start(mdWrapper);

                        DoLog($"{Config.Name}--> Recv Market Data:{msg.ToString()}", Constants.MessageType.Information);
                    }
                    else
                    {
                        DoLog($"Ignoring market data for unknown symbol {msg.Symbol}",Constants.MessageType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"ERROR processing market data {ex.Message}",Constants.MessageType.Information);
            }
        }
        
        public  void ProcessExecutionReport(ExecutionReportDTO msg)
        {
            try
            {
                lock (JsonOrdersDict)
                {

                    if (JsonOrdersDict.ContainsKey(msg.ClOrdId))
                    {
                        DoLog($"{Config.Name}--> Recv ExecutionReport:{msg.ToString()}",
                            Constants.MessageType.Information);
                        NewOrderReq order = JsonOrdersDict[msg.ClOrdId];
                        ExecutionReportWrapper execReportWrapper = new ExecutionReportWrapper(msg, order);
                        (new Thread(ProcessExecutionReportAsync)).Start(execReportWrapper);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name} - CRITICAL ERROR processing execution report:{ex.Message}",Constants.MessageType.Error);
            }
        }

        #endregion

        #region Incoming Process Methods

        protected void ProcessUpdateOrderRequest(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper) param;
                
                string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);
                string origClOrdId = (string)wrapper.GetField(OrderFields.OrigClOrdID);
                double price = (double)wrapper.GetField(OrderFields.Price);

                UpdateOrderReq updReq = new UpdateOrderReq()
                {
                    OrigClOrdId = origClOrdId,
                    ClOrdId = clOrdId,
                    Price = price
                };

                lock (JsonOrdersDict)
                {
                    if (JsonOrdersDict.ContainsKey(origClOrdId))
                    {
                        NewOrderReq toUpdOrderReq = JsonOrdersDict[origClOrdId];
                        NewOrderReq updated = toUpdOrderReq.Clone();
                        updated.ClOrdId = clOrdId;
                        updated.Price = price;
                        JsonOrdersDict.Add(clOrdId, updated);

                        DoSendAsync<NewOrderReq>(updReq);
                    }
                    else
                    {
                        OrderCancelRejectWrapper cxlRplRejWapper = new OrderCancelRejectWrapper(clOrdId, null,
                            $"Could not find ClOrdId {origClOrdId} as a managed order",
                            CxlRejReason.UnknownOrder, CxlRejResponseTo.OrderCancelReplaceRequest);

                        (new Thread(ProcessIncomingAsync)).Start(cxlRplRejWapper);
                    }
                }

            }
            catch (Exception e)
            {
                DoLog($"ERROR updating order:{e.Message}",Constants.MessageType.Error);
            }
        }

        protected void ProcessCancelOrderRequest(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper) param;
                
                string clOrdId = (string)wrapper.GetField(OrderFields.ClOrdID);

                CancelOrderReq cxlReq = new CancelOrderReq() {ClOrderId = clOrdId, OrigClOrderId = clOrdId};

                lock (JsonOrdersDict)
                {
                    if (JsonOrdersDict.ContainsKey(clOrdId))
                    {
                        DoSendAsync<CancelOrderReq>(cxlReq);
                    }
                    else
                    {
                        OrderCancelRejectWrapper cxlRejWapper = new OrderCancelRejectWrapper(clOrdId, null,
                            $"Could not find ClOrdId {clOrdId} as a managed order",
                            CxlRejReason.TooLateToCancel, CxlRejResponseTo.OrderCancelRequest);

                        (new Thread(ProcessIncomingAsync)).Start(cxlRejWapper);
                    }
                }

            }
            catch (Exception ex)
            {
                DoLog($"ERROR canceling order : {ex.Message}",Constants.MessageType.Information);
            }
            
        }

        protected void ProcessNewOrderRequest(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper) param;
                Order newOrder = WrapperOrderConverter.ConvertNewOrder(wrapper);
                NewOrderReq newOrderReq = JsonOrderConverter.ConvertNewOrder(newOrder);;

                lock (JsonOrdersDict)
                {
                    JsonOrdersDict.Add(newOrderReq.ClOrdId, newOrderReq);
                }
                
                DoSendAsync<NewOrderReq>(newOrderReq);
            }
            catch (Exception ex)
            {
                DoLog($"ERROR sending new order to market: {ex.Message}",Constants.MessageType.Information);
            }
        }

        protected  void ProcessMarketDataRequest(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper) param;
                string symbol = (string) wrapper.GetField(MarketDataRequestField.Symbol);
                long mdReq = Convert.ToInt64( wrapper.GetField(MarketDataRequestField.MDReqId));

                if (symbol == null)
                    throw new Exception($"Could not find a symbol for market data request");

                lock (MarketDataRequests)
                {

                    if (MarketDataRequests.ContainsKey(symbol))
                        MarketDataRequests.Remove(symbol);

                    MarketDataRequests.Add(symbol, mdReq);
                }
                

                WebSocketSubscribeMessage subscr = new WebSocketSubscribeMessage()
                {
                    Msg = "Subscribe",
                    SubscriptionType = WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_SUBSCRIBE,
                    Service = "MD",
                    ServiceKey = symbol,
                    UUID = Guid.NewGuid().ToString()
                };
                DoLog($"Subscribing Market Data for symbol {symbol}", Constants.MessageType.Information);
                DoSendAsync<WebSocketSubscribeMessage>(subscr);
            }
            catch (Exception ex)
            {
                DoLog($"ERROR sending market data request: {ex.Message}",Constants.MessageType.Information);
            }
        }
        
        protected  void ProcessCandlebarRequest(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper) param;
                
                string symbol = (string) wrapper.GetField(MarketDataRequestField.Symbol);
                long mdReq = Convert.ToInt64( wrapper.GetField(MarketDataRequestField.MDReqId));

                if (symbol == null)
                    throw new Exception($"Could not find a symbol for market data request");

                lock (CandlebarRequests)
                {

                    if (CandlebarRequests.ContainsKey(symbol))
                        CandlebarRequests.Remove(symbol);

                    CandlebarRequests.Add(symbol, mdReq);
                }
                

                WebSocketSubscribeMessage subscr = new WebSocketSubscribeMessage()
                {
                    Msg = "Subscribe",
                    SubscriptionType = WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_SUBSCRIBE,
                    Service = "CB",
                    ServiceKey = symbol,
                    UUID = Guid.NewGuid().ToString()
                };
                
                string strReq = JsonConvert.SerializeObject(subscr, Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                
                WebSocketClient.Send(strReq);

            }
            catch (Exception ex)
            {
                DoLog($"ERROR sending market data request: {ex.Message}",Constants.MessageType.Information);
            }
        }

        #endregion
        
        #region ICommunication Methods

        void ISingletonModule.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            DoLoadConfig(configFile, listaCamposSinValor);
        }

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        public void SetOutgoingEvent(OnMessageReceived OnMessageRcv)
        {
            OnExecutionReportMessageRcv += OnMessageRcv;
        }

        public void SetIncomingEvent(OnMessageReceived OnMessageRcv)
        {
            OnIncomingMessageRcv += OnMessageRcv;
        }

        void ILogger.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            DoLoadConfig(configFile, listaCamposSinValor);
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration().GetConfiguration<tph.StrategyHandler.SimpleCommandSender.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            if (wrapper.GetAction() == Actions.MARKET_DATA_REQUEST)
            {
                DoLog("Processing Market Data Request:" + wrapper.ToString(), Constants.MessageType.Information);
                (new Thread(ProcessMarketDataRequest)).Start(wrapper);
                DoLog("Market Data Request successfully processed:" + wrapper.ToString(), Constants.MessageType.Information);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.CANDLE_BAR_REQUEST)
            {
                DoLog("Processing Candlebar Request:" + wrapper.ToString(), Constants.MessageType.Information);
                (new Thread(ProcessMarketDataRequest)).Start(wrapper);
                DoLog("Candlebar Request successfully processed:" + wrapper.ToString(), Constants.MessageType.Information);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.NEW_ORDER)
            {
                DoLog("Processing New Order Request:" + wrapper.ToString(), Constants.MessageType.Information);
                (new Thread(ProcessNewOrderRequest)).Start(wrapper);
                DoLog("New Order Request Successfully:" + wrapper.ToString(), Constants.MessageType.Information);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
            {
                DoLog("Processing Cxl Order Request:" + wrapper.ToString(), Constants.MessageType.Information);
                (new Thread(ProcessCancelOrderRequest)).Start(wrapper);
                DoLog("Cxl Order Request Successfully processed:" + wrapper.ToString(), Constants.MessageType.Information);
                return CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
            {
                
                DoLog("Processing Cxl Repl. Order Request:" + wrapper.ToString(), Constants.MessageType.Information);
                (new Thread(ProcessUpdateOrderRequest)).Start(wrapper);
                DoLog("Cxl Repl. Order Request Successfully processed:" + wrapper.ToString(), Constants.MessageType.Information);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
            {
                //return OrderRouterModule.ProcessMessage(wrapper);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.ORDER_MASS_STATUS_REQUEST)
            {
                //return OrderRouterModule.ProcessMessage(wrapper);
                return  CMState.BuildSuccess();
            }
            else
            {
                OnMessageRcv(wrapper);
                DoLog(string.Format("Invoking OnMessageRcv @SimpleCommandSender for action {0}",wrapper.GetAction()),Constants.MessageType.Error);
                return CMState.BuildSuccess();    
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                //this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    //Build the  trading modules
                    DoLog(string.Format("Initializing SimpleCommSender module @{0}",DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")),Constants.MessageType.Information);
                    
                    MarketDataRequests= new Dictionary<string, long>();
                    CandlebarRequests=new Dictionary<string, long>();
                    WrapperOrderConverter = new OrderConverter();
                    JsonOrderConverter = new tph.StrategyHandler.SimpleCommandSender.Common.Util.OrderConverter();
                    JsonOrdersDict=new Dictionary<string, NewOrderReq>();
                    
                    //Finish starting up the server
                    WebSocketClient = new WebSocketClient(Config.WebSocketURL,
                        ProcessEvent, ProcessMarketData,ProcessCandlebar, ProcessExecutionReport,
                        DoLog);
                    WebSocketClient.Connect();

                    DoLog("Websocket successfully initialized on URL:  " + Config, Constants.MessageType.Information);
                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critical error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }
        
        #endregion
    }
}
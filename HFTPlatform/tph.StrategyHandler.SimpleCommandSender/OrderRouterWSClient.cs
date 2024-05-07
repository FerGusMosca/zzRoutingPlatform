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
using tph.StrategyHandler.SimpleCommandSender.Common.Util;
using tph.StrategyHandler.SimpleCommandSender.Common.Wrappers;
using tph.StrategyHandler.SimpleCommandSender.ServiceLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.OrderRouters.Common.Wrappers;
using zHFT.SingletonModulesHandler.Common.Interfaces;
using zHFT.StrategyHandler.Common.Wrappers;
using OrderConverter = zHFT.OrderRouters.Common.Converters.OrderConverter;

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
        
        protected Dictionary<string,long> HistoricalPricesRequests { get; set; }
        
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
                        //DoLog($"Ignoring candlebar for unknown symbol {msg.Symbol}", Constants.MessageType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"ERROR processing candlebar {ex.Message}",Constants.MessageType.Information);
            }
        }

        public void ProcessHistoricalPrices(HistoricalPricesDTO msg)
        {
            try
            {
                lock (HistoricalPricesRequests)
                {
                    if (HistoricalPricesRequests.ContainsKey(msg.Symbol))
                    {
                        Security mainSec = new Security() {Symbol = msg.Symbol};
                        List<Wrapper> mdWrappers = new List<Wrapper>();
                        foreach (MarketData md in msg.MarketData)
                        {
                            Security sec = new Security() {Symbol = msg.Symbol};
                            sec.MarketData = md;
                            MarketDataWrapper mdWrapper = new MarketDataWrapper(sec, Config);
                            mdWrappers.Add(mdWrapper);
                        }

                        HistoricalPricesWrapper histWrapper = new HistoricalPricesWrapper(msg.ReqId,mainSec, msg.Interval, mdWrappers);
                        DoLog($"{Config.Name}--> Recv {mdWrappers.Count} prices for symbol :{mainSec.Symbol}",
                            Constants.MessageType.Information);
                        (new Thread(ProcessIncomingAsync)).Start(histWrapper);
                    }
                    else
                    {
                        DoLog($"Ignoring historical prices for unknown symbol {msg.Symbol}",
                            Constants.MessageType.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"ERROR processing historical prices {ex.Message}",Constants.MessageType.Error);
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
                        DoLog($"Ignoring market data for unknown symbol {msg.Symbol}",Constants.MessageType.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"ERROR processing market data {ex.Message}",Constants.MessageType.Error);
            }
        }

        public void ProcessSecurityList(SecurityListDTO msg)
        {
            try
            {
                List<Wrapper> securityWrapperList = new List<Wrapper>();

                foreach (Security sec in msg.Securities)
                {
                    SecurityWrapper secWrapper = new SecurityWrapper(sec, Config);
                    securityWrapperList.Add(secWrapper);
                
                }


                DoLog($"{Config.Name}-->Received security list for Security List Req. Id {msg.SecurityListRequestId}--> Type={msg.SecurityListRequestType}", Constants.MessageType.Information);
                SecurityListWrapper wrapper = new SecurityListWrapper(msg.SecurityListRequestId,
                                                                      securityWrapperList,
                                                                      msg.SecurityListRequestType, msg.Market);
                (new Thread(ProcessIncomingAsync)).Start(wrapper);
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name} - CRITICAL ERROR processing security list:{ex.Message}", Constants.MessageType.Error);
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

                UpdateOrderReq updReq= SimpleCommandSender.Common.Util.OrderConverter.ConvertUpdateOrderReq(wrapper);


                lock (JsonOrdersDict)
                {
                    if (JsonOrdersDict.ContainsKey(updReq.OrigClOrdId))
                    {
                        NewOrderReq toUpdOrderReq = JsonOrdersDict[updReq.OrigClOrdId];
                        NewOrderReq updated = toUpdOrderReq.Clone();
                        updated.ClOrdId = updReq.ClOrdId;
                        updated.Price = updReq.Price;

                        if(!JsonOrdersDict.ContainsKey(updReq.ClOrdId))
                            JsonOrdersDict.Add(updReq.ClOrdId, updated);
                        else
                            JsonOrdersDict[updReq.ClOrdId] = updated;

                        DoLog($"{Config.Name}--> Updating Symbol {toUpdOrderReq.Symbol} price: {toUpdOrderReq.Price}-> {updated.Price}", Constants.MessageType.PriorityInformation);
                        DoSendAsync<UpdateOrderReq>(updReq);
                    }
                    else
                    {
                        OrderCancelRejectWrapper cxlRplRejWapper = new OrderCancelRejectWrapper(updReq.ClOrdId, null,
                            $"Could not find ClOrdId {updReq.OrigClOrdId} as a managed order",
                            CxlRejReason.UnknownOrder, CxlRejResponseTo.OrderCancelReplaceRequest);

                        (new Thread(ProcessIncomingAsync)).Start(cxlRplRejWapper);
                    }
                }

            }
            catch (Exception e)
            {
                DoLog($"{Config.Name}-->ERROR updating order:{e.Message}",Constants.MessageType.Error);
            }
        }

        protected void ProcessSecurityListRequest(object param)
        {
            try
            {
                SecurityListRequestWrapper wrapper = (SecurityListRequestWrapper)param;
                SecurityListReqDTO reqDTO = SecurityListRequestConverter.ConvertSecurityListRequest(wrapper);
                DoSendAsync<SecurityListReqDTO>(reqDTO);

            }
            catch (Exception ex)
            {
                DoLog($"ERROR requesting security list  : {ex.Message}", Constants.MessageType.Information);
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

        protected void ProcessHistoricalPricesRequest(object param)
        {
            try
            {
                HistoricalPricesReqDTO reqDTO=HistoricalPricesRequestConverter.ConvertHistoricalPricesRequest( (Wrapper) param);
                string cleanSymbol = reqDTO.Symbol;
                reqDTO.Symbol= FullSymbolManager.BuildFullSymbol(reqDTO.Symbol, reqDTO.Exchange, reqDTO.SecurityType);

                lock (HistoricalPricesRequests)
                {
                    if (HistoricalPricesRequests.ContainsKey(cleanSymbol))
                        HistoricalPricesRequests.Remove(cleanSymbol);
                    HistoricalPricesRequests.Add(cleanSymbol, reqDTO.HistPrReqId);
                }
                
                DoLog($"Subscribing historical prices for symbol {reqDTO.Symbol}", Constants.MessageType.Information);
                DoSendAsync<HistoricalPricesReqDTO>(reqDTO);
            }
            catch (Exception ex)
            {
                DoLog($"ERROR sending historical prices request: {ex.Message}",Constants.MessageType.Information);
            }
            
        }

        protected void ProcessMarketDataReqDict(string symbol,string exchange, long mdReq)
        {

            lock (MarketDataRequests)
            {

                if (MarketDataRequests.ContainsKey(symbol))
                    MarketDataRequests.Remove(symbol);

                MarketDataRequests.Add(symbol, mdReq);

                string fullSymbol = FullSymbolManager.BuildSemiFullSymbol(symbol, exchange);

                if(MarketDataRequests.ContainsKey(fullSymbol))
                    MarketDataRequests.Remove(fullSymbol);

                MarketDataRequests.Add(fullSymbol, mdReq);

            }
        }

        protected  void ProcessMarketDataRequest(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper) param;
                string symbol = (string) wrapper.GetField(MarketDataRequestField.Symbol);
                long mdReq = Convert.ToInt64( wrapper.GetField(MarketDataRequestField.MDReqId));
                string exchange = (string)wrapper.GetField(MarketDataRequestField.Exchange);
                SecurityType? securityType = (SecurityType?)wrapper.GetField(MarketDataRequestField.SecurityType);

                string fullSymbol = FullSymbolManager.BuildFullSymbol(symbol, exchange, securityType);


                if (fullSymbol == null)
                    throw new Exception($"Could not find a symbol for market data request");


                ProcessMarketDataReqDict(symbol, exchange, mdReq);

                WebSocketSubscribeMessage subscr = new WebSocketSubscribeMessage()
                {
                    Msg = "Subscribe",
                    SubscriptionType = WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_SUBSCRIBE,
                    Service = "MD",
                    ServiceKey = fullSymbol,
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
            else if (wrapper.GetAction() == Actions.HISTORICAL_PRICES_REQUEST)
            {
                DoLog("Processing Historical Prices Request:" + wrapper.ToString(), Constants.MessageType.Information);
                (new Thread(ProcessHistoricalPricesRequest)).Start(wrapper);
                DoLog("Historical Prices Request successfully processed:" + wrapper.ToString(), Constants.MessageType.Information);
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
            else if (wrapper.GetAction() == Actions.SECURITY_LIST_REQUEST)
            {
                DoLog("Processing Security List Request:" + wrapper.ToString(), Constants.MessageType.Information);
                (new Thread(ProcessSecurityListRequest)).Start(wrapper);
                DoLog("Security List Request Successfully processed:" + wrapper.ToString(), Constants.MessageType.Information);
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
                    HistoricalPricesRequests=new Dictionary<string, long>();
                    CandlebarRequests=new Dictionary<string, long>();
                    WrapperOrderConverter = new OrderConverter();
                    JsonOrderConverter = new tph.StrategyHandler.SimpleCommandSender.Common.Util.OrderConverter();
                    JsonOrdersDict=new Dictionary<string, NewOrderReq>();
                    
                    //Finish starting up the server
                    WebSocketClient = new WebSocketClient(Config.WebSocketURL,
                        ProcessEvent, ProcessMarketData,ProcessCandlebar, ProcessExecutionReport,
                        ProcessHistoricalPrices,ProcessSecurityList,DoLog);
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
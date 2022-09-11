using System;
using System.Security;
using System.Threading;
using Newtonsoft.Json;
using OrderBookLoaderMock.Common.DTO;
using OrderBookLoaderMock.Common.DTO.Orders;
using OrderBookLoaderMock.Common.Interfaces;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace OrderBookLoaderMock.LogicLayer
{
    public class MarketDataSubcriptionLogic
    {
        #region Public Attributes
        
        protected  WebSocketClient MarketDataWebSocketClient { get; set; }
        
        protected object EventsLock { get; set; }
        
        protected bool OrdersSynchronizerThreadStarted { get; set; }
        
        protected IMarketDataPublication OnMarketData { get; set; }
        
        protected  ILogger Logger { get; set; }
        
        #endregion
        
        #region Constructors

        public MarketDataSubcriptionLogic(string mockWS,IMarketDataPublication pOnMarketData,ILogger pLogger )
        {
            MarketDataWebSocketClient= new WebSocketClient(mockWS,OnWebsocketMsg,OnMarketDataEv,OnExecutionReportEv);

            MarketDataWebSocketClient.Connect();
            
            EventsLock= new object();

            OrdersSynchronizerThreadStarted = false;
            
            OnMarketData = pOnMarketData;

            Logger = pLogger;
        }

        #endregion
        
        #region Protected Methods
        
        protected void DoLog(string msg, Constants.MessageType type)
        {
            if (Logger != null)
                Logger.DoLog(msg, type);

        }
        
        private  void DoSend<T>(T obj)
        {
            string strMsg = JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
            MarketDataWebSocketClient.Send(strMsg);
        }
        
        #endregion
        
        #region Public Methods
        
        public void OnExecutionReportEv(ExecutionReportMsg er)
        {
            lock (EventsLock)
            {
                DoLog(string.Format("Recv exec report for symbol {0}:{1}", er.Order.Symbol, er.ToString()),
                    Constants.MessageType.Information);
            }
        }

        
        public void OnMarketDataEv(MarketDataMsg md)
        {
            DoLog(string.Format("Recv market data for symbol {0}:{1}", md.Security.Symbol, md.ToString()),
                Constants.MessageType.Information);

            lock (EventsLock)
            {
                OnMarketData.OnMarketData(md);
            }
        }

        
        
        public async  void OnWebsocketMsg(WebSocketMessage msg)
        {
            try
            {
                lock (EventsLock)
                {
                    if (msg.Msg == "OrderBookMsg")
                    {
                       
                        OnMarketData.ProcessEvent(msg);

                    }

                    else if (msg.Msg == "OrderCancelRejectMsg")
                    {
                        OrderCancelRejectMsg ocr = (OrderCancelRejectMsg) msg;
                        DoLog(string.Format("Recv order cancel reject for ClOrdId {0}:{1}", ocr.OrigClOrdId, ocr.Text),
                            Constants.MessageType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                string errMsg = string.Format("Warning processing incoming message:{0}",ex.Message);
                DoLog(errMsg, Constants.MessageType.Error);
            }
        }
        
        public void SubscribeOrderBook(string symbol)
        {
            try
            {
                DoLog(string.Format("Subscribing order book for symbol {0}", symbol),Constants.MessageType.Information);

                WebSocketSubscribeMessage subscMsg = new WebSocketSubscribeMessage()
                {
                    Msg = "Subscribe",
                    Service = WebSocketSubscribeMessage._ORDER_BOOK_SERVICE,
                    SubscriptionType = WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_SUBSCRIBE,
                    ServiceKey = symbol,
                    UUID = Guid.NewGuid().ToString()
                };

                DoSend<WebSocketSubscribeMessage>(subscMsg);
                
                DoLog(string.Format("Order book subscribed for symbol {0}", symbol),Constants.MessageType.Information);
            
            }
            catch (Exception e)
            {
                DoLog(string.Format("Order book subscription error for symbol {0}:{1}", symbol,e.Message),Constants.MessageType.Error);
            }
        }
        
        #endregion
    }
}
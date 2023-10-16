using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderBookLoaderMock.Common.DTO;
using OrderBookLoaderMock.Common.DTO.Generic;
using OrderBookLoaderMock.Common.DTO.Orders;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;

namespace tph.StrategyHandler.SimpleCommandSender.ServiceLayer
{
    public delegate void ProcessEvent(WebSocketMessage msg);
    
    public delegate void ProcessMarketData(MarketDataMsg msg);
    
    public delegate void ProcessCandlebar(CandlebarMsg msg);
    
    public delegate void ProcessExecutionReport(ExecutionReportMsg msg);
    
    public class WebSocketClient
    {
        #region Protected Attributes

        protected string WebSocketURL { get; set; }

        protected ProcessEvent OnEvent { get; set; }
        
        protected ProcessMarketData OnMarketData { get; set; }
        
        protected ProcessCandlebar OnCandlebar { get; set; }
        protected ProcessExecutionReport OnExecutionReport { get; set; }

        protected ClientWebSocket SubscriptionWebSocket { get; set; }
        
        protected  object tLock { get; set; }

        #endregion
        
        #region Constructors

        public WebSocketClient(string pWebSocketURL, ProcessEvent pOnEvent,ProcessMarketData pOnMarketData,
                                ProcessCandlebar pOnProcessCandlebar,
                                ProcessExecutionReport pOnExecutionReport)
        {
            WebSocketURL = pWebSocketURL;
            OnEvent = pOnEvent;
            OnMarketData = pOnMarketData;
            OnCandlebar = pOnProcessCandlebar;
            OnExecutionReport = pOnExecutionReport;
            
            tLock=new object();
        }

        #endregion
        
        #region Public Methods
        
        public bool Connect()
        {

            SubscriptionWebSocket = new ClientWebSocket();
            SubscriptionWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

            Thread respThread = new Thread(ReadResponses);
            respThread.Start(new object[] { });
            return true;
        }
        
        public virtual  void ReadResponses(object param)
        {
            while (true)
            {
                try
                {
                    string resp = "";
                    WebSocketReceiveResult webSocketResp;
                    if (SubscriptionWebSocket.State == WebSocketState.Open)
                    {
                        lock (tLock)
                        {
                            do
                            {

                                ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1000]);
                                webSocketResp = SubscriptionWebSocket
                                    .ReceiveAsync(bytesReceived, CancellationToken.None).Result;
                                resp += Encoding.ASCII.GetString(bytesReceived.Array, 0, webSocketResp.Count);
                            } while (!webSocketResp.EndOfMessage);
                        }

                        if (resp != "")
                        {
                            //Console.Beep();
                            WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(resp);
                            
                            if (wsResp.Msg == "SubscriptionResponse")
                            {
                                SubscriptionResponse subscrResponse = JsonConvert.DeserializeObject<SubscriptionResponse>(resp);
                                OnEvent(subscrResponse);
                            }
                            else if (wsResp.Msg == "OrderCancelRejectMsg")
                                OnEvent(JsonConvert.DeserializeObject<OrderCancelRejectMsg>(resp));
                            else if (wsResp.Msg == "OrderBookMsg")
                                OnEvent(JsonConvert.DeserializeObject<OrderBookMsg>(resp));
                            else if (wsResp.Msg == "OrderCancelResponse")
                                OnEvent(JsonConvert.DeserializeObject<OrderCancelResponse>(resp));
                            else if (wsResp.Msg == "NewOrderResponse")
                                OnEvent(JsonConvert.DeserializeObject<NewOrderResponse>(resp));
                            else if (wsResp.Msg == "MarketDataMsg")
                                OnMarketData(JsonConvert.DeserializeObject<MarketDataMsg>(resp));
                            else if (wsResp.Msg == "CandlebarMsg")
                                OnCandlebar(JsonConvert.DeserializeObject<CandlebarMsg>(resp));
                            else if (wsResp.Msg == "ExecutionReportMsg")
                                OnExecutionReport(JsonConvert.DeserializeObject<ExecutionReportMsg>(resp));
                            else if (wsResp.Msg == "OrderCancelRejectMsg")
                                OnEvent(JsonConvert.DeserializeObject<OrderCancelRejectMsg>(resp));
                            else if (wsResp.Msg == "UnsubscriptionResponse")
                                OnEvent(JsonConvert.DeserializeObject<SubscriptionResponse>(resp));
                            else if (wsResp.Msg == "ClientDepthOfBook")
                                OnEvent(JsonConvert.DeserializeObject<SubscriptionResponse>(resp));
                            else if (wsResp.Msg == "ClientHeartbeat")
                            {

                            }
                            else
                            {
                                UnknownMessage unknownMsg = new UnknownMessage()
                                {
                                    Msg = "UnknownMsg",
                                    Resp = resp,
                                    Reason = string.Format("Unknown message: {0}", resp)
                                };
                                OnEvent(unknownMsg);
                            }
                        }
                    }
                    else
                    {
                        //Console.Beep(1000,2000);
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage errorMsg = new ErrorMessage() { Msg = "ErrorMsg", Error = ex.Message };
                    OnEvent(errorMsg);
                }
            }
        }
        
        public  void Send(string strMsg)
        {
            try
            {
                if(SubscriptionWebSocket.State!=WebSocketState.Open)
                    throw new Exception($"Websocket is in status {SubscriptionWebSocket.State} and it should be OPEN!");
                
                byte[] msgArray = Encoding.ASCII.GetBytes(strMsg);

                ArraySegment<byte> bytesToSend = new ArraySegment<byte>(msgArray);

                lock (tLock)
                {
                    Thread.Sleep(1);//Amazing how this solves everything?
                    SubscriptionWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                string msg = $"CRITICAL ERROR sending message through the websocket: {ex.Message}";
            }
        }
        


        #endregion
    }
}
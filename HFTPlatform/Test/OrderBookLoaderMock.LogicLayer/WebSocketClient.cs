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

namespace OrderBookLoaderMock.LogicLayer
{
    
    public delegate void ProcessEvent(WebSocketMessage msg);
    
    public delegate void ProcessMarketData(MarketDataMsg msg);
    
    public delegate void ProcessExecutionReport(ExecutionReportMsg msg);
    
    public class WebSocketClient
    {        
        #region Protected Attributes

        protected string WebSocketURL { get; set; }

        protected ProcessEvent OnEvent { get; set; }
        
        protected ProcessMarketData OnMarketData { get; set; }
        
        protected ProcessExecutionReport OnExecutionReport { get; set; }

        protected ClientWebSocket SubscriptionWebSocket { get; set; }

        #endregion

        #region Constructors

        public WebSocketClient(string pWebSocketURL, ProcessEvent pOnEvent,ProcessMarketData pOnMarketData,
                                ProcessExecutionReport pOnExecutionReport)
        {
            WebSocketURL = pWebSocketURL;
            OnEvent = pOnEvent;
            OnMarketData = pOnMarketData;
            OnExecutionReport = pOnExecutionReport;
        }

        #endregion

        #region Public Methods

        public async Task<bool> Connect()
        {

            SubscriptionWebSocket = new ClientWebSocket();
            await SubscriptionWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

            Thread respThread = new Thread(ReadResponses);
            respThread.Start(new object[] { });
            return true;
        }

        public virtual async void ReadResponses(object param)
        {
            while (true)
            {
                try
                {
                    string resp = "";
                    WebSocketReceiveResult webSocketResp;
                    if (SubscriptionWebSocket.State == WebSocketState.Open)
                    {
                        do
                        {
                            ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1000]);
                            webSocketResp = await SubscriptionWebSocket.ReceiveAsync(bytesReceived, CancellationToken.None);
                            resp += Encoding.ASCII.GetString(bytesReceived.Array, 0, webSocketResp.Count);
                        }
                        while (!webSocketResp.EndOfMessage);

                        if (resp != "")
                        {
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
//                            else if (wsResp.Msg == "AuthenticationResp")
//                                OnEvent(JsonConvert.DeserializeObject<AuthenticationResp>(resp));
                            else if (wsResp.Msg == "NewOrderResponse")
                                OnEvent(JsonConvert.DeserializeObject<NewOrderResponse>(resp));
                            else if (wsResp.Msg == "MarketDataMsg")
                                OnMarketData(JsonConvert.DeserializeObject<MarketDataMsg>(resp));
                            else if (wsResp.Msg == "ExecutionReportMsg")
                                OnExecutionReport(JsonConvert.DeserializeObject<ExecutionReportMsg>(resp));
                            else if (wsResp.Msg == "OrderCancelRejectMsg")
                                OnEvent(JsonConvert.DeserializeObject<OrderCancelRejectMsg>(resp));
                            else if (wsResp.Msg == "UnsubscriptionResponse")
                                OnEvent(JsonConvert.DeserializeObject<SubscriptionResponse>(resp));
                            else if (wsResp.Msg == "ClientDepthOfBook")
                            {
                                ClientDepthOfBook dpthBook = JsonConvert.DeserializeObject<ClientDepthOfBook>(resp);
                                OnEvent(dpthBook);
                                //ClientDepthOfBook
                            }
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
                        Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    ErrorMessage errorMsg = new ErrorMessage() { Msg = "ErrorMsg", Error = ex.Message };
                    OnEvent(errorMsg);
                }
            }
        }

        public async void Send(string strMsg)
        {
            byte[] msgArray = Encoding.ASCII.GetBytes(strMsg);

            ArraySegment<byte> bytesToSend = new ArraySegment<byte>(msgArray);

            await SubscriptionWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                                                          CancellationToken.None);

        }

        #endregion
    }
    
}
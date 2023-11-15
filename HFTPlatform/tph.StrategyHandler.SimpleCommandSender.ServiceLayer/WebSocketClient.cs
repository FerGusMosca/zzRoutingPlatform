using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace tph.StrategyHandler.SimpleCommandSender.ServiceLayer
{
    public delegate void ProcessEvent(WebSocketMessage msg);
    
    public delegate void ProcessMarketData(MarketDataDTO msg);
    
    public delegate void ProcessCandlebar(CandlebarDTO msg);
    
    public delegate void ProcessExecutionReport(ExecutionReportDTO msg);
    
    public delegate void ProcessHistoricalPrices(HistoricalPricesDTO msg);

    public delegate void ProcessSecurityList(SecurityListDTO msg);


    public class WebSocketClient
    {
        #region Protected Attributes

        protected string WebSocketURL { get; set; }

        protected ProcessEvent OnEvent { get; set; }
        
        protected ProcessMarketData OnMarketData { get; set; }
        
        protected ProcessCandlebar OnCandlebar { get; set; }
        protected ProcessExecutionReport OnExecutionReport { get; set; }
        
        protected ProcessHistoricalPrices OnHistoricalPrices { get; set; }

        protected ProcessSecurityList OnSecurityList { get; set; }

        protected  OnLogMessage OnLogMessage { get; set; }

        protected ClientWebSocket SubscriptionWebSocket { get; set; }
        
        protected  ConcurrentQueue<string> MessagesQueue { get; set; }
        
        protected  object tLock { get; set; }

        #endregion
        
        #region Constructors

        public WebSocketClient(string pWebSocketURL, ProcessEvent pOnEvent,ProcessMarketData pOnMarketData,
                                ProcessCandlebar pOnProcessCandlebar,
                                ProcessExecutionReport pOnExecutionReport,
                                ProcessHistoricalPrices pOnHistoricalPrices,
                                ProcessSecurityList pOnProcessSecurityList,
                                OnLogMessage pOnLogMessage)
        {
            WebSocketURL = pWebSocketURL;
            OnEvent = pOnEvent;
            OnMarketData = pOnMarketData;
            OnCandlebar = pOnProcessCandlebar;
            OnExecutionReport = pOnExecutionReport;
            OnHistoricalPrices = pOnHistoricalPrices;
            OnSecurityList= pOnProcessSecurityList;
            OnLogMessage = pOnLogMessage;
            
            MessagesQueue=new ConcurrentQueue<string>();
            
            tLock=new object();
        }

        #endregion
        
        #region Public Methods
        
        public bool Connect()
        {

            SubscriptionWebSocket = new ClientWebSocket();
            SubscriptionWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

            while (SubscriptionWebSocket.State != WebSocketState.Open)
            {
                OnLogMessage($"Waiting for the websocket to connect to {WebSocketURL}",
                    Constants.MessageType.Information);
                Thread.Sleep(100);
            }


            OnLogMessage($"Websocket client successfully connected", Constants.MessageType.Information);

            (new Thread(ReadResponses)).Start(new object[] { });
            (new Thread(SendMessages)).Start(new object[] { });
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
                     
                        do
                        {

                            ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1000]);
                            webSocketResp = SubscriptionWebSocket
                                .ReceiveAsync(bytesReceived, CancellationToken.None).Result;
                            resp += Encoding.ASCII.GetString(bytesReceived.Array, 0, webSocketResp.Count);
                        } while (!webSocketResp.EndOfMessage);
                    

                        if (resp != "")
                        {
                            //Console.Beep();
                            WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(resp);
                            
                            if (wsResp.Msg == "SubscriptionResponse")
                            {
                                SubscriptionResponse subscrResponse = JsonConvert.DeserializeObject<SubscriptionResponse>(resp);
                                OnEvent(subscrResponse);
                            }
//                            else if (wsResp.Msg == "OrderCancelRejectMsg")
//                                OnEvent(JsonConvert.DeserializeObject<OrderCancelRejectMsg>(resp));
//                            else if (wsResp.Msg == "OrderBookMsg")
//                                OnEvent(JsonConvert.DeserializeObject<OrderBookMsg>(resp));
                            else if (wsResp.Msg == "OrderCancelResponse")
                                OnEvent(JsonConvert.DeserializeObject<CancelOrderAck>(resp));
                            else if (wsResp.Msg == "NewOrderAck")
                                OnEvent(JsonConvert.DeserializeObject<NewOrderAck>(resp));
                            else if (wsResp.Msg == "UpdOrderAck")
                                OnEvent(JsonConvert.DeserializeObject<UpdateOrderAck>(resp));
                            else if (wsResp.Msg == "OrderMassStatusRequestAck")
                                OnEvent(JsonConvert.DeserializeObject<OrderMassStatusRequestAck>(resp));
                            else if (wsResp.Msg == "MarketDataMsg")
                                OnMarketData(JsonConvert.DeserializeObject<MarketDataDTO>(resp));
                            else if (wsResp.Msg == "CandlebarMsg")
                                OnCandlebar(JsonConvert.DeserializeObject<CandlebarDTO>(resp));
                            else if (wsResp.Msg == "SecurityListMsg")
                                OnSecurityList(JsonConvert.DeserializeObject<SecurityListDTO>(resp));
                            else if (wsResp.Msg == "ExecutionReportMsg")
                            {
                                ExecutionReport execRep = JsonConvert.DeserializeObject<ExecutionReport>(resp);
                                ExecutionReportDTO dto = new ExecutionReportDTO(execRep);
                                OnExecutionReport(dto);
                            }
                            else if (wsResp.Msg == "HistoricalPricesMsg")
                            {
                                HistoricalPricesDTO histDTO = JsonConvert.DeserializeObject<HistoricalPricesDTO>(resp);
                                OnHistoricalPrices(histDTO);
                            }
//                            else if (wsResp.Msg == "OrderCancelRejectMsg")
//                                OnEvent(JsonConvert.DeserializeObject<OrderCancelRejectDTO>(resp));
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
                    OnLogMessage(ex.Message, Constants.MessageType.Error);
                    ErrorMessage errorMsg = new ErrorMessage() { Msg = "ErrorMsg", Error = ex.Message };
                    OnEvent(errorMsg);
                }
            }
        }


        protected void DoSend(string strMsg)
        {
            try
            {
                //if(SubscriptionWebSocket.State!=WebSocketState.Open)
                //    throw new Exception($"Websocket is in status {SubscriptionWebSocket.State} and it should be OPEN!");
                
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
                Console.Beep(1000,2000);
                OnLogMessage(msg, Constants.MessageType.Error);
            }
        }

        public void SendMessages(object param)
        {
            try
            {
                while (true)
                {
                    while (MessagesQueue.IsEmpty)
                    {
                        Thread.Sleep(10);
                    }
                    
                    lock (MessagesQueue)
                    {
                        string entry = null;
                        while (MessagesQueue.TryDequeue(out entry))
                        {
                            if (entry != null)
                                DoSend(entry);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"CRITICAL ERROR sending message dequeued from queue MessagesQueue:{ex.Message}",
                    Constants.MessageType.Error);
            }
            
        }

        public  void Send(string strMsg)
        {
            try
            {
                lock (MessagesQueue)
                {
                    MessagesQueue.Enqueue(strMsg);
                }
            }
            catch (Exception ex)
            {
                OnLogMessage($"CRITICAL error Enqueueing message to Websocket:{ex.Message}",
                    Constants.MessageType.Error);

            }
        }
        


        #endregion
    }
}
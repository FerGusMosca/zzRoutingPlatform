using System;
using System.Collections.Generic;
using System.Threading;
using Fleck;
using Newtonsoft.Json;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace tph.StrategyHandler.SimpleCommandReceiver.DataAccessLayer
{
    public class WebSocketServer:WebSocketBaseServer
    {
        #region Constructors

        public WebSocketServer(string pURL, ILogger pLogger, OnMessageReceived pOnMessageReceived)
        {
            URL = pURL;

            ConnectedClients = new Dictionary<int, IWebSocketConnection>();

            HeartbeatThreads = new Dictionary<int, Thread>();

            Logger = pLogger;

            OnMessageReceived = pOnMessageReceived;

            Subscriptions = new Dictionary<int, Dictionary<string, WebSocketSubscribeMessage>>();

            DoLog("Initializing Websocket server...", Constants.MessageType.Information);

            MdReqId = 0;


        }

        #endregion

        #region Private Static Consts

        private static string _ORDER_BOOK_SERVICE = "OB";
        
        private static string _MARKET_DATA_SERVICE = "MD";

        #endregion

        #region Protected Attributes
        
        protected long MdReqId { get; set; }

        protected Dictionary<int, Dictionary<string, WebSocketSubscribeMessage>> Subscriptions { get; set; }

        #endregion

        #region Private Methods

        protected bool IsSubscribed(IWebSocketConnection socket, string service, string serviceKey = null)
        {
            if (Subscriptions.ContainsKey(socket.ConnectionInfo.ClientPort))
            {
                if (serviceKey == null)
                    return Subscriptions[socket.ConnectionInfo.ClientPort].ContainsKey(service);
                else
                {
                    Dictionary<string, WebSocketSubscribeMessage> subscrDict = Subscriptions[socket.ConnectionInfo.ClientPort];

                    if (subscrDict.ContainsKey(service))
                        return subscrDict[service].ServiceKey == serviceKey;
                    else
                        return false;
                }
            }
            else
                return false;
        }

        protected void SubscribeService(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {

            if (!Subscriptions.ContainsKey(socket.ConnectionInfo.ClientPort))
            {
                Dictionary<string, WebSocketSubscribeMessage> services = new Dictionary<string, WebSocketSubscribeMessage>();
                services.Add(subscrMsg.Service, subscrMsg);
                Subscriptions.Add(socket.ConnectionInfo.ClientPort, services);

            }
            else
            {
                Dictionary<string, WebSocketSubscribeMessage> subscritMsgs = Subscriptions[socket.ConnectionInfo.ClientPort];

                if (!subscritMsgs.ContainsKey(subscrMsg.Service))
                    subscritMsgs.Add(subscrMsg.Service, subscrMsg);
                else
                    subscritMsgs[subscrMsg.Service] = subscrMsg;
            }
        }

        protected void UnsubscribeService(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            DoLog(string.Format("Unsubscribing client {0} for service {1}",socket.ConnectionInfo.ClientPort,subscrMsg.Service),Constants.MessageType.Information);
            if (Subscriptions[socket.ConnectionInfo.ClientPort] != null)
            {
                if (Subscriptions[socket.ConnectionInfo.ClientPort].ContainsKey(subscrMsg.Service))
                {
                    Subscriptions[socket.ConnectionInfo.ClientPort].Remove(subscrMsg.Service);
                    DoLog(string.Format("Unsubscribed client {0} for service {1}", socket.ConnectionInfo.ClientPort, subscrMsg.Service), Constants.MessageType.Information);
                }
                else
                    DoLog(string.Format("Could not find subscription to  service {1} for client {0} ", socket.ConnectionInfo.ClientPort, subscrMsg.Service),Constants.MessageType.Information);

            }
            else
                throw new Exception(string.Format("No suscriptions for client Id {0}", socket.ConnectionInfo.ClientPort));
        }

        #endregion

        #region Protected Methods

        protected void ProcessMarketDataRequest(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            SubscribeService(socket, subscrMsg);

            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MdReqId,
                                                                        new Security() {Symbol = subscrMsg.ServiceKey},
                                                                        SubscriptionRequestType.SnapshotAndUpdates,
                                                                        MarketDepth.TopOfBook);
            MdReqId++;
            CMState reqState = OnMessageReceived(wrapper);


            ProcessSubscriptionResponse(socket, subscrMsg.Service, subscrMsg.ServiceKey, subscrMsg.UUID,
                reqState.Success, reqState.Exception != null ? reqState.Exception.Message : null);

        }

        protected void ProcessOrderBookRequest(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            SubscribeService(socket, subscrMsg);
            
            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MdReqId,
                                                                            new Security() {Symbol = subscrMsg.ServiceKey},
                                                                            SubscriptionRequestType.SnapshotAndUpdates,
                                                                            MarketDepth.FullBook);
            MdReqId++;
            CMState reqState = OnMessageReceived(wrapper);


            ProcessSubscriptionResponse(socket, subscrMsg.Service, subscrMsg.ServiceKey, subscrMsg.UUID,
                reqState.Success, reqState.Exception != null ? reqState.Exception.Message : null);
        }

        protected void ProcessSubscriptions(IWebSocketConnection socket, string m)
        {
            WebSocketSubscribeMessage subscrMsg = JsonConvert.DeserializeObject<WebSocketSubscribeMessage>(m);

            DoLog(string.Format("Incoming subscription for service {0} - ServiceKey:{1}", subscrMsg.Service, subscrMsg.ServiceKey), Constants.MessageType.Information);

            if (subscrMsg.SubscriptionType == WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_SUBSCRIBE)
            {
                if (subscrMsg.Service == _ORDER_BOOK_SERVICE)
                {
                    ProcessOrderBookRequest(socket, subscrMsg);
                }
                if (subscrMsg.Service == _MARKET_DATA_SERVICE)
                {
                    ProcessMarketDataRequest(socket, subscrMsg);
                }
                else
                {
                    throw new Exception(string.Format("Subscription method not available:{0}",subscrMsg.Service));
                }
                
            }
            else if (subscrMsg.SubscriptionType == WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_UNSUBSCRIBE)
            {
                UnsubscribeService(socket, subscrMsg);
                //unsubscriptions don't have confirms
            }
        }

        #endregion

        #region Public Methods

        public void PublishEntity<T>(T entity)
        {
            try
            {

                lock (ConnectedClients)
                {
                    foreach (IWebSocketConnection ConnectionSocket in ConnectedClients.Values)
                    {
                        try
                        {
                            if (ConnectionSocket != null && ConnectionSocket.IsAvailable)
                            {
                                DoSend<T>(ConnectionSocket,entity);
                            }
                        }
                        catch (Exception ex)
                        {
                            DoLog(string.Format("Critical error  @PublishEntity: {0}", ex.Message), Constants.MessageType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error sending error message @PublishEntity. Error= {0}", ex.Message),Constants.MessageType.Error);
            }
        }
        
        #endregion

        #region Websocket Methods

        protected override void OnMessage(IWebSocketConnection socket, string m)
        {
            try
            {
                WebSocketMessage wsResp = JsonConvert.DeserializeObject<WebSocketMessage>(m);

                if (wsResp.Msg == "Subscribe")
                {
                    ProcessSubscriptions(socket, m);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Websocket Server : Exception processing onMessage:{0}", ex.Message), Constants.MessageType.Error);
                ErrorMessage errorMsg = new ErrorMessage()
                {
                    Msg = "MessageReject",
                    Error = string.Format("Error processing message: {0}", ex.Message)

                };
                DoSend<ErrorMessage>(socket, errorMsg);
            }
        
        }

        #endregion
    }
}
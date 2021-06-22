using System;
using System.Collections.Generic;
using System.Threading;
using Fleck;
using Newtonsoft.Json;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace tph.StrategyHandler.SimpleCommandReceiver.DataAccessLayer
{
    public abstract class WebSocketBaseServer
    {
                #region Protected Attributes

        protected Dictionary<int, IWebSocketConnection> ConnectedClients { get; set; }

        protected Dictionary<int, Thread> HeartbeatThreads { get; set; }

        protected string URL { get; set; }

        protected Fleck.WebSocketServer WebSocketServer { get; set; }

        protected ILogger Logger { get; set; }

        protected OnMessageReceived OnMessageReceived { get; set; }

        #endregion

        #region Protected Methods

        protected void DoLog(string message,Constants.MessageType type) {
            if (Logger != null)
                Logger.DoLog(message, type);
        
        }

        protected void DoSend<T>(IWebSocketConnection socket, T entity)
        {
            lock (ConnectedClients)
            {

                if (socket != null && socket.IsAvailable)
                {
                    string strMsg = JsonConvert.SerializeObject(entity, Newtonsoft.Json.Formatting.None,
                                      new JsonSerializerSettings
                                      {
                                          NullValueHandling = NullValueHandling.Ignore
                                      });

                    socket.Send(strMsg);
                }
                else
                    Logger.DoLog(string.Format("Discarding message for client {0} because is no longer connected",
                                socket != null && socket.ConnectionInfo != null ? socket.ConnectionInfo.ClientIpAddress : "?"),
                                Constants.MessageType.Information);
            }

        }

    

        protected virtual void OnOpen(IWebSocketConnection socket)
        {
            try
            {
                lock (ConnectedClients)
                {

                    if (!ConnectedClients.ContainsKey(socket.ConnectionInfo.ClientPort))
                    {

                        DoLog(string.Format("Connecting websocket client {0}", socket.ConnectionInfo.ClientPort), Constants.MessageType.Information);
                        //socket.Send("Connection Opened");
                        ConnectedClients.Add(socket.ConnectionInfo.ClientPort, socket);

                        DoLog(string.Format("EquityMonitor.Websocket: Client {0} Connected ", socket.ConnectionInfo.ClientPort), Constants.MessageType.Information);
                    }
                    else
                        throw new Exception(string.Format("Connection already exists for client {0}", socket.ConnectionInfo.ClientPort));
                }
            }
            catch (Exception ex)
            {
                if (socket != null && socket.ConnectionInfo != null)
                    DoLog(string.Format("EquityMonitor.Websocket: Exception at  OnOpen for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message),Constants.MessageType.Error);
                else
                    DoLog(string.Format("EquityMonitor.Websocket: Exception at  OnOpen for unknown client {0}", ex.Message), Constants.MessageType.Error);


            }
        }

        protected virtual void OnClose(IWebSocketConnection socket)
        {
            try
            {
                lock (ConnectedClients)
                {

                    if (ConnectedClients.ContainsKey(socket.ConnectionInfo.ClientPort))
                        ConnectedClients.Remove(socket.ConnectionInfo.ClientPort);

                    if (HeartbeatThreads.ContainsKey(socket.ConnectionInfo.ClientPort))
                        HeartbeatThreads[socket.ConnectionInfo.ClientPort].Abort();

                    DoLog(string.Format("EquityMonitor.Websocket:  OnClose for client {0}", socket.ConnectionInfo.ClientPort), Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                if (socket != null && socket.ConnectionInfo != null && socket.ConnectionInfo.ClientPort != null)
                    DoLog(string.Format("EquityMonitor.Websocket: Exception at  OnClose for client {0}: {1}", socket.ConnectionInfo.ClientPort, ex.Message), Constants.MessageType.Error);
                else
                    DoLog(string.Format("EquityMonitor.Websocket: Exception at  OnClose for unknown client: {0}", ex.Message), Constants.MessageType.Error);

            }
        }

        protected void ProcessSubscriptionResponse(IWebSocketConnection socket, string service, string serviceKey, string UUID, bool success = true, string msg = "")
        {
            SubscriptionResponse resp = new SubscriptionResponse()
            {
                Message = msg,
                Success = success,
                Service = service,
                ServiceKey = serviceKey,
                UUID = UUID,
                Msg = "SubscriptionResponse"

            };

            DoLog(string.Format("SubscriptionResponse UUID:{0} Service:{1} ServiceKey:{2} Success:{3}", resp.UUID, resp.Service, resp.ServiceKey, resp.Success),Constants.MessageType.Information);
            DoSend<SubscriptionResponse>(socket, resp);
        }


        #endregion

        #region Abstract Methods

        protected abstract void OnMessage(IWebSocketConnection socket, string m);

        #endregion

        #region Public Methods

        public void Start()
        {
            WebSocketServer = new Fleck.WebSocketServer(URL);
            WebSocketServer.Start(socket =>
            {
                socket.OnOpen = () => OnOpen(socket);
                socket.OnClose = () => OnClose(socket);
                socket.OnMessage = m => OnMessage(socket, m);

            });

        }

        #endregion
    }
}
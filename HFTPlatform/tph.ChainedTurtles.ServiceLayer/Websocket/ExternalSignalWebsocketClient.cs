using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.Interfaces;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace tph.ChainedTurtles.ServiceLayer.Websocket
{
    public class ExternalSignalWebsocketClient : IExternalSignalClient
    {

        #region Private Static Conts

        private static string _WEBSOCKET_URL_KEY = "WebSocketURL";

        #endregion

        #region Protected Attributes

        protected ClientWebSocket ClientWebSocket { get; set; }

        protected Dictionary<string, string> ConfigDictionary { get; set; }

        protected string WebSocketURL{get;set;}

        protected OnLogMessage OnLogMessage { get; set; }

        protected ConcurrentQueue<string> MessagesQueue { get; set; }

        protected object tLock { get; set; }

        #endregion

        #region Constructors

        public ExternalSignalWebsocketClient(Dictionary<string, string> pConfigDict, OnLogMessage pOnLogMessage) 
        {

            ConfigDictionary = pConfigDict;
            OnLogMessage += pOnLogMessage;

            MessagesQueue = new ConcurrentQueue<string>();

            tLock = new object();

            LoadConfigValues();
        }

        #endregion

        #region Private Methods

        private void LoadConfigValues()
        {
            if (ConfigDictionary.ContainsKey(_WEBSOCKET_URL_KEY))
                WebSocketURL = ConfigDictionary[_WEBSOCKET_URL_KEY];
            else
                throw new Exception($"Could not find key {_WEBSOCKET_URL_KEY} in commConfig attribute");
        }

        protected void DoSend(string strMsg)
        {
            try
            {
                byte[] msgArray = Encoding.ASCII.GetBytes(strMsg);

                ArraySegment<byte> bytesToSend = new ArraySegment<byte>(msgArray);

                lock (tLock)
                {
                    Thread.Sleep(1);//Amazing how this solves everything?

                    ClientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {

                string msg = $"CRITICAL ERROR sending message through the websocket: {ex.Message}";
                Console.Beep(1000, 2000);
                OnLogMessage(msg, Constants.MessageType.Error);
            }
        }
        public virtual void ReadResponses(object param)
        { 
        
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


        #endregion

        #region Public Methods

        public bool Connect()
        {
            ClientWebSocket = new ClientWebSocket();

            ClientWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

            while (ClientWebSocket.State != WebSocketState.Open)
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


        public string EvalSignal(string featuresPayload)
        {
            return null;
        }


        #endregion
    }
}

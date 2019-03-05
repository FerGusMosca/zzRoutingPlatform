using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets.Auth;
using zHFT.Main.Common.Interfaces;

namespace zHFT.FullMrktConnectivity.BitMex.DAL.WebSockets
{
    public abstract class BaseManager
    {
        #region Public Static Consts

        public static string _INSTRUMENT = "instrument";
        public static string _ORDER = "order";
        public static string _EXECUTIONS = "execution";
        public static string _ORDERBOOK_L2 = "orderBookL2";
        public static string _TRADE = "trade";
        public static string _1_DAY_TRADE_BINS = "tradeBin1d";
        protected static string _VERB = "GET";
        protected static string _ENDPOINT = "/realtime";

        #endregion

        #region Protected Static Attributes

        //protected static ClientWebSocket SubscriptionWebSocket { get; set; }

        protected  string WebSocketURL { get; set; }

        protected  UserCredentials UserCredentials { get; set; }

        protected  bool PendingRequestResponse { get; set; }

        protected  bool Initialized = false;

        protected  Dictionary<string, WebSocketSubscriptionResponse> ResponseRequestSubscriptions { get; set; }

        protected  Dictionary<string, WebSocketSubscriptionEvent> EventSubscriptions { get; set; }

        protected  object tSubscrLock = new object();

        public  AuthenticationResult AuthSubscriptionResult { get; set; }

        protected string ID { get; set; }

        protected string Secret { get; set; }

     
        #endregion

        #region Constructors

        public BaseManager(string pWebSocketURL, UserCredentials pUserCredentials)
        {
            WebSocketURL = pWebSocketURL;
            UserCredentials = pUserCredentials;
            PendingRequestResponse = false;
        }

        public BaseManager()
        {
            PendingRequestResponse = false;
        }

        #endregion

        #region Abstract Methods

        protected  abstract void DoRunLoopSubscriptions(string resp);

        #endregion

        #region Subscription Methods

        public  void OnEvent(string resp)
        {
            WebSocketResponseMessage wsResp = wsResp = JsonConvert.DeserializeObject<WebSocketResponseMessage>(resp);
            lock (tSubscrLock)
            {

                if (wsResp.IsResponse())//We have the response to a subscription
                {
                    WebSocketSubscriptionResponse requestRespSubscr = JsonConvert.DeserializeObject<WebSocketSubscriptionResponse>(resp);

                    if (ResponseRequestSubscriptions.ContainsKey(requestRespSubscr.GetSubscriptionEvent()))
                    {
                        WebSocketSubscriptionResponse requestRespEvent = ResponseRequestSubscriptions[requestRespSubscr.GetSubscriptionEvent()];

                        requestRespEvent.RunSubscritionEvent(requestRespSubscr);
                    }

                }
                else //We have an event from a subscription
                {
                    WebSocketSubscriptionResponse requestRespSubscr = JsonConvert.DeserializeObject<WebSocketSubscriptionResponse>(resp);

                    if (requestRespSubscr.IsAuthentication())
                    {
                        if (requestRespSubscr.success)
                        {
                            AuthSubscriptionResult.Authenticated = true;
                            //Good idea to calculate here the expiration of the token
                            AuthSubscriptionResult.ExpiresIn = Convert.ToInt32(requestRespSubscr.request.args[1]);
                        }
                        else
                        {
                            AuthSubscriptionResult.Authenticated = false;

                            AuthSubscriptionResult.ErrorMessage = requestRespSubscr.error;

                            //abort = true;//we cannot continue requesting info if there was an authentication problem
                            //even for public serivces. Unless with this architecture
                        }
                    }
                    else
                    {
                        DoRunLoopSubscriptions(resp);
                    }

                }
            }
        }


        #endregion

        #region Protected Methods

        protected  async Task InvokeWebSocket(WebSocketSubscriptionRequest req)
        {
            if (AuthSubscriptionResult.ClientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    string strReq = JsonConvert.SerializeObject(req, Newtonsoft.Json.Formatting.None,
                                                                new JsonSerializerSettings
                                                                {
                                                                    NullValueHandling = NullValueHandling.Ignore
                                                                });

                    byte[] msgArray = Encoding.ASCII.GetBytes(strReq);

                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(msgArray);

                    await AuthSubscriptionResult.ClientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                                                                            CancellationToken.None);

                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
                throw new Exception(string.Format("Not connected to BitMex on subscription channel:{0}", AuthSubscriptionResult.ClientWebSocket.State.ToString()));
        }

        protected  async Task InvokeAuthWebSocket(WebSocketAuthenticationRequest req)
        {
            if (AuthSubscriptionResult.ClientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    string strReq = JsonConvert.SerializeObject(req, Newtonsoft.Json.Formatting.None,
                                                                new JsonSerializerSettings
                                                                {
                                                                    NullValueHandling = NullValueHandling.Ignore
                                                                });

                    byte[] msgArray = Encoding.ASCII.GetBytes(strReq);

                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(msgArray);

                    await AuthSubscriptionResult.ClientWebSocket.SendAsync(bytesToSend, WebSocketMessageType.Text, true,
                                                                            CancellationToken.None);

                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
                throw new Exception(string.Format("Not connected to BitMex on subscription channel:{0}", AuthSubscriptionResult.ClientWebSocket.State.ToString()));
        }

        protected  async void RunLoopSubscriptions()
        {
            bool abort = false;
            while (!abort)
            {
                try
                {
                    while (AuthSubscriptionResult == null || AuthSubscriptionResult.ClientWebSocket == null || AuthSubscriptionResult.ClientWebSocket.State != WebSocketState.Open)
                        Thread.Sleep(1000);

                    string resp = "";
                    WebSocketReceiveResult webSocketResp;

                    do
                    {
                        ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1000]);
                        webSocketResp = await AuthSubscriptionResult.ClientWebSocket.ReceiveAsync(bytesReceived, CancellationToken.None);
                        resp += Encoding.ASCII.GetString(bytesReceived.Array, 0, webSocketResp.Count);
                    }
                    while (!webSocketResp.EndOfMessage);

                    OnEvent(resp);

                }
                catch (Exception ex)
                {
                    //TODO: Log fatal exception receiving data from subscription
                    abort = true;
                }
            }
        }

        protected  async Task<Welcome> GetWelcomeMessage(ClientWebSocket WebSocketToUse)
        {

            if (WebSocketToUse.State == WebSocketState.Open)
            {
                ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult result = await WebSocketToUse.ReceiveAsync(bytesReceived, CancellationToken.None);
                string resp = Encoding.ASCII.GetString(bytesReceived.Array, 0, result.Count);
                Welcome welcome = JsonConvert.DeserializeObject<Welcome>(resp);

                return welcome;
            }
            else
                throw new Exception(string.Format("Not connected to BitMex:{0}", WebSocketToUse.State.ToString()));

        }

        protected  string SignHMACSHA256(string secret, string verb, string endpoint, int expires)
        {

            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] key = utf8.GetBytes(secret);

            string data = "";//nothing to send

            //TODO: Ver la conersión que hace en el demo de Python para la URL
            string message = verb + endpoint + expires.ToString() + data;

            byte[] bMessage = utf8.GetBytes(message);

            HMACSHA256 hasher = new HMACSHA256(key);

            byte[] hashedMessage = hasher.ComputeHash(bMessage);

            string hexSignature = BitConverter.ToString(hashedMessage).Replace("-", "");

            return hexSignature;
        }

        protected  async Task<AuthenticationResult> DoAuthenticate()
        {
            if (AuthSubscriptionResult == null)
            {
                AuthSubscriptionResult = await ConnectSubscriptions();
            }

            lock (tSubscrLock)
            {
                if (!AuthSubscriptionResult.Authenticated)
                {

                    //1- A UNIX timestamp after which the request is no longer valid. This is to prevent replay attacks.
                    //In the example, we create an authentication valid for 12 hours
                    Int32 expires = (Int32)(DateTime.UtcNow.AddHours(12).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    //2-api-key: Your public API key. This the id param returned when you create an API Key via the AP
                    string api_key = UserCredentials.BitMexID;
                    string api_secret = UserCredentials.BitMexSecret;

                    //3-api-signature: A signature of the request you are making.
                    //It is calculated as hex(HMAC_SHA256(apiSecret, verb + path + expires + data)). 
                    string hexSignature = SignHMACSHA256(api_secret, _VERB, _ENDPOINT, expires);

                    WebSocketAuthenticationRequest autRequest = new WebSocketAuthenticationRequest()
                    {
                        op = "authKeyExpires",
                        args = new object[] { api_key, expires, hexSignature }

                    };

                    InvokeAuthWebSocket(autRequest).Wait();
                }
            }

            return AuthSubscriptionResult;
        }

        protected  async Task<AuthenticationResult> AuthenticateSubscriptions()
        {
            if (AuthSubscriptionResult == null || !AuthSubscriptionResult.Authenticated)
            {
                try
                {
                    AuthSubscriptionResult = await DoAuthenticate();

                    if (!AuthSubscriptionResult.Authenticated && !string.IsNullOrEmpty(AuthSubscriptionResult.ErrorMessage))
                        throw new Exception(AuthSubscriptionResult.ErrorMessage);
                    else if (!AuthSubscriptionResult.Success && string.IsNullOrEmpty(AuthSubscriptionResult.ErrorMessage))
                        throw new Exception("Unknown error authenticating on Subscriptions Web Socket");
                    else
                    {
                        if (!Initialized)
                        {
                            Initialize();
                            Initialized = true;
                        }
                    }

                    return AuthSubscriptionResult;

                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error authenticating on Subscriptions Web Socket @{0}:{1}:", WebSocketURL, ex.Message));
                }
            }
            else
                return AuthSubscriptionResult;
        }

        #endregion

        #region Public Static Methods

        public  void Initialize()
        {
            ResponseRequestSubscriptions = new Dictionary<string, WebSocketSubscriptionResponse>();
            EventSubscriptions = new Dictionary<string, WebSocketSubscriptionEvent>();
            RunLoopSubscriptions();
        }

        public  void SubscribeResponseRequest(string topic, OnSubscritionEvent pOnSubscriptionEvent,
                                                    object[] parameters = null)
        {

            WebSocketSubscriptionResponse msg = new WebSocketSubscriptionResponse()
            {
                topic = topic,
                parameters = parameters,
            };

            msg.SetSubscritionEvent(pOnSubscriptionEvent);

            if (ResponseRequestSubscriptions == null)
                throw new Exception(string.Format("Response Request Subscriptions Dictionary has not been initialized"));
            ResponseRequestSubscriptions.Add(topic, msg);
        }

        public  void SubscribeEvents(string topic, OnEvent pOnSubscriptionEvent)
        {

            WebSocketSubscriptionEvent msg = new WebSocketSubscriptionEvent()
            {
                topic = topic,
            };

            msg.SetEvent(pOnSubscriptionEvent);

            if (EventSubscriptions == null)
                throw new Exception(string.Format("Event Subscriptions Dictionary has not been initialized"));
            EventSubscriptions.Add(topic, msg);
        }

        public  async Task<AuthenticationResult> ConnectSubscriptions()
        {
            if (!Initialized)
            {
                try
                {
                    //1-The client connects to the server and performs the WebSocket handshake.
                    ClientWebSocket clientWebSocket = new ClientWebSocket();
                    await clientWebSocket.ConnectAsync(new Uri(WebSocketURL), CancellationToken.None);

                    Welcome welcomResp = await GetWelcomeMessage(clientWebSocket);

                    if (welcomResp.info.Contains("Welcome"))
                    {

                        AuthSubscriptionResult = new AuthenticationResult()
                        {
                            Success = true,
                            Authenticated = false,
                            Welcome = welcomResp,
                            ClientWebSocket = clientWebSocket
                        };
                        Initialize();
                        Initialized = true;
                        return AuthSubscriptionResult;
                    }
                    else
                    {
                        AuthSubscriptionResult = new AuthenticationResult() { Success = false, Authenticated = false, ErrorMessage = welcomResp.info };
                        return AuthSubscriptionResult;
                    }
                }
                catch (Exception ex)
                {
                    AuthSubscriptionResult = new AuthenticationResult() { Success = false, Authenticated = false, ErrorMessage = ex.Message };
                    return AuthSubscriptionResult;
                }
            }
            else
                return AuthSubscriptionResult;
        }

        #endregion
    }
}

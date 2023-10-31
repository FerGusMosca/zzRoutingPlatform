using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;
using Fleck;
using Newtonsoft.Json;
using tph.StrategyHandler.SimpleCommandReceiver.Common.Configuration;
using tph.StrategyHandler.SimpleCommandReceiver.Common.Converters;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;
using CancelOrderWrapper = tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper.CancelOrderWrapper;
using MarketDataRequestWrapper = tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper.MarketDataRequestWrapper;
using UpdateOrderWrapper = tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper.UpdateOrderWrapper;

namespace tph.StrategyHandler.SimpleCommandReceiver.DataAccessLayer
{
    
    public delegate void OnSubscribeMarketData(Security security);

    public delegate void OnSubscribeCandlebars(Security security);

    public class WebSocketServer : WebSocketBaseServer
    {
        #region Constructors

        public WebSocketServer(string pURL, ILogger pLogger, OnMessageReceived pOnMessageReceived,
                                OnSubscribeMarketData pOnSubscribeMarketData = null, OnSubscribeCandlebars pOnSubscribeCandlebars = null,
                                bool pSimulateCandlebars = false)
        {
            URL = pURL;

            ConnectedClients = new Dictionary<int, IWebSocketConnection>();

            HeartbeatThreads = new Dictionary<int, Thread>();

            Logger = pLogger;

            OnMessageReceived = pOnMessageReceived;

            OnSubscribeCandlebars = pOnSubscribeCandlebars;

            OnSubscribeMarketData = pOnSubscribeMarketData;

            Subscriptions = new Dictionary<int, Dictionary<string, WebSocketSubscribeMessage>>();

            DoLog("Initializing Websocket server...", Constants.MessageType.Information);

            MdReqId = 0;

            SimulateCandlebars = pSimulateCandlebars;


        }

        #endregion

        #region Private Static Consts

        private static string _ORDER_BOOK_SERVICE = "OB";

        private static string _MARKET_DATA_SERVICE = "MD";

        private static string _CANDLEBAR_SERVICE = "CB";

        #endregion

        #region Protected Attributes

        protected int MdReqId { get; set; }

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
            DoLog(string.Format("Unsubscribing client {0} for service {1}", socket.ConnectionInfo.ClientPort, subscrMsg.Service), Constants.MessageType.Information);
            if (Subscriptions[socket.ConnectionInfo.ClientPort] != null)
            {
                if (Subscriptions[socket.ConnectionInfo.ClientPort].ContainsKey(subscrMsg.Service))
                {
                    Subscriptions[socket.ConnectionInfo.ClientPort].Remove(subscrMsg.Service);
                    DoLog(string.Format("Unsubscribed client {0} for service {1}", socket.ConnectionInfo.ClientPort, subscrMsg.Service), Constants.MessageType.Information);
                }
                else
                    DoLog(string.Format("Could not find subscription to  service {1} for client {0} ", socket.ConnectionInfo.ClientPort, subscrMsg.Service), Constants.MessageType.Information);

            }
            else
                throw new Exception(string.Format("No suscriptions for client Id {0}", socket.ConnectionInfo.ClientPort));
        }

        #endregion

        #region Protected Methods

        protected void ProcessMarketDataRequest(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            SubscribeService(socket, subscrMsg);

            Security sec = OrderConverter.GetSecurityFullSymbol(subscrMsg.ServiceKey);



            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MdReqId, sec,
                                                                        SubscriptionRequestType.SnapshotAndUpdates,
                                                                        MarketDepth.TopOfBook);
            MdReqId++;
            CMState reqState = OnMessageReceived(wrapper);

            if (reqState.Success && OnSubscribeMarketData != null)
                OnSubscribeMarketData(sec);


            ProcessSubscriptionResponse(socket, subscrMsg.Service, subscrMsg.ServiceKey, subscrMsg.UUID,
                reqState.Success, reqState.Exception != null ? reqState.Exception.Message : null);

        }

        protected void ProcessCandlebarRequest(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            SubscribeService(socket, subscrMsg);

            Security sec = OrderConverter.GetSecurityFullSymbol(subscrMsg.ServiceKey);

            Wrapper wrapper = null;

            if (SimulateCandlebars)
            {

                wrapper = new MarketDataRequestWrapper(MdReqId, sec, SubscriptionRequestType.SnapshotAndUpdates, MarketDepth.TopOfBook);
                MdReqId++;
            }
            else
            {
                throw new Exception(string.Format("Candlebar subscription not implemented without simulation"));
                //Simulation is when we ask market data but return candlebars
            }

            CMState reqState = OnMessageReceived(wrapper);

            if (reqState.Success && OnSubscribeCandlebars != null)
                OnSubscribeCandlebars(sec);


            ProcessSubscriptionResponse(socket, subscrMsg.Service, subscrMsg.ServiceKey, subscrMsg.UUID,
                reqState.Success, reqState.Exception != null ? reqState.Exception.Message : null);

        }

        protected void ProcessOrderBookRequest(IWebSocketConnection socket, WebSocketSubscribeMessage subscrMsg)
        {
            SubscribeService(socket, subscrMsg);

            Security sec = OrderConverter.GetSecurityFullSymbol(subscrMsg.ServiceKey);

            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MdReqId, sec,
                                                                            SubscriptionRequestType.SnapshotAndUpdates,
                                                                            MarketDepth.FullBook);
            MdReqId++;
            CMState reqState = OnMessageReceived(wrapper);


            ProcessSubscriptionResponse(socket, subscrMsg.Service, subscrMsg.ServiceKey, subscrMsg.UUID,
                reqState.Success, reqState.Exception != null ? reqState.Exception.Message : null);
        }

        protected void ProcessCancelAllReq(IWebSocketConnection socket, string m)
        {
            CancelAllReq cxlAllReq = JsonConvert.DeserializeObject<CancelAllReq>(m);
            try
            {

                DoLog(string.Format("Incoming Cancel All Req for reason {0}", cxlAllReq.Reason), Constants.MessageType.Information);

                CancelAllWrapper cxlAllWrapper = new CancelAllWrapper();

                CMState resp = OnMessageReceived(cxlAllWrapper);

                if (resp.Success)
                {
                    CancelOrderAck ackMsg = new CancelOrderAck()
                    {
                        Msg = "CancelOrderAck",
                        Success = true,
                    };

                    DoSend<CancelOrderAck>(socket, ackMsg);

                    DoLog(string.Format("Cancel All Order Req for Reaeson {0}  successfully processed", cxlAllReq.Reason), Constants.MessageType.Information);
                }
                else
                    throw resp.Exception;

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR for Cancel All Req Req for Reason {0}. Error:{1}", cxlAllReq.Reason, ex.Message), Constants.MessageType.Error);

                CancelOrderAck ackMsg = new CancelOrderAck()
                {
                    Msg = "RouteOrderAck",
                    Success = false,
                    Error = ex.Message

                };
            }
        }

        protected void ProcessSecurityListRequest(IWebSocketConnection socket, string m)
        {

            try
            {
                DoLog(string.Format("Incoming ProcessSecurityListRequest Request"), Constants.MessageType.Information);

                SecurityListReqDTO dto = JsonConvert.DeserializeObject<SecurityListReqDTO>(m);

                SecurityListRequestWrapper hprWrapper = new SecurityListRequestWrapper(dto.SecurityListRequestType, dto.Symbol,
                                                                                        dto.SecurityType,dto.Exchange,dto.Currency);

                CMState resp = OnMessageReceived(hprWrapper);

                if (resp.Success)
                {
                    SecurityListReqAckDTO ackMsg = new SecurityListReqAckDTO()
                    {
                        Success = true,
                    };

                    DoSend<SecurityListReqAckDTO>(socket, ackMsg);
                    DoLog(string.Format("Security List Request Request  successfully processed"), Constants.MessageType.Information);
                }
                else
                    throw resp.Exception;

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR for Security List Request. Error:{0}", ex.Message), Constants.MessageType.Error);

                SecurityListReqAckDTO ackMsg = new SecurityListReqAckDTO()
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }


        protected void ProcessHistoricalPricesRequest(IWebSocketConnection socket, string m)
        {
            try
            {
                DoLog(string.Format("Incoming HistoricalPricesRequest Request"), Constants.MessageType.Information);

                HistoricalPricesReqDTO dto =JsonConvert.DeserializeObject<HistoricalPricesReqDTO>(m);

                HistoricalPricesRequestWrapper hprWrapper = new HistoricalPricesRequestWrapper(dto.HistPrReqId,
                    dto.Symbol,
                    dto.From, dto.To, dto.GetCandleInterval());
                
                CMState resp = OnMessageReceived(hprWrapper);
                
                if (resp.Success)
                {
                    HistoricalPricesReqAckDTO ackMsg = new HistoricalPricesReqAckDTO()
                    {
                        Success = true,
                    };

                    DoSend<HistoricalPricesReqAckDTO>(socket, ackMsg);
                    DoLog(string.Format("Historical Prices Request  successfully processed"), Constants.MessageType.Information);
                }
                else
                    throw resp.Exception;

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR for Historical Prices Request. Error:{0}",ex.Message), Constants.MessageType.Error);

                HistoricalPricesReqAckDTO ackMsg = new HistoricalPricesReqAckDTO()
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        protected void ProcessOrderMassStatusRequest(IWebSocketConnection socket, string m)
        {
            try
            {
                
                DoLog(string.Format("Incoming Order Mass Status Request"), Constants.MessageType.Information);

                OrderMassStatusRequestWrapper omsrWrapper = new OrderMassStatusRequestWrapper();
                
                CMState resp = OnMessageReceived(omsrWrapper);
                
                if (resp.Success)
                {
                    OrderMassStatusRequestAck ackMsg = new OrderMassStatusRequestAck()
                    {
                        Success = true,
                    };

                    DoSend<OrderMassStatusRequestAck>(socket, ackMsg);
                    DoLog(string.Format("Order Mass Status Request  successfully processed"), Constants.MessageType.Information);
                }
                else
                    throw resp.Exception;

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR for Order Mass Status Request. Error:{0}",ex.Message), Constants.MessageType.Error);

                OrderMassStatusRequestAck ackMsg = new OrderMassStatusRequestAck()
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        protected void ProcessUpdateOrderReq(IWebSocketConnection socket, string m)
        {
            UpdateOrderReq updOrderReq = JsonConvert.DeserializeObject<UpdateOrderReq>(m);
            try
            {

                DoLog(string.Format("Incoming  Order Update Req for ClOrdId {0} (Qty={1} Price={2})",
                        updOrderReq.OrigClOrdId, updOrderReq.Qty.HasValue ? updOrderReq.Qty.Value.ToString() : "-",
                        updOrderReq.Price.HasValue ? updOrderReq.Price.Value.ToString() : "-"),
                        Constants.MessageType.Information);

                Order order = new Order()
                {
                    Symbol = updOrderReq.Symbol,
                    OrigClOrdId = updOrderReq.OrigClOrdId,
                    ClOrdId = updOrderReq.ClOrdId,
                    OrderId = updOrderReq.OrderId,
                    OrderQty = updOrderReq.Qty,
                    Price = updOrderReq.Price,
                    TimeInForce = updOrderReq.TimeInForce,
                    Side = updOrderReq.Side,
                    OrdType = updOrderReq.OrdType,
                    
                };
                
                UpdateOrderWrapper updOrderReqWrapper= new UpdateOrderWrapper(order);
                CMState resp = OnMessageReceived(updOrderReqWrapper);
                
                if (resp.Success)
                {
                    UpdateOrderAck ackMsg = new UpdateOrderAck()
                    {
                        Success = true,
                    };

                    DoSend<UpdateOrderAck>(socket, ackMsg);

                    DoLog(string.Format("Update Order Req for ClOrdId {0}  successfully processed", updOrderReq.OrigClOrdId), Constants.MessageType.Information);
                }
                else
                    throw resp.Exception;

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR for Update Order Req Req for ClOrdId {0}. Error:{1}", updOrderReq.OrigClOrdId,ex.Message), Constants.MessageType.Error);

                UpdateOrderAck ackMsg = new UpdateOrderAck()
                {
                    
                    Success = false,
                    Error = ex.Message

                };
            }
        }

        protected void ProcessCancelOrderReq(IWebSocketConnection socket, string m)
        {
            CancelOrderReq cxlOrderReq = JsonConvert.DeserializeObject<CancelOrderReq>(m);
            try
            {
                
                DoLog(string.Format("Incoming  Order Cxl Req for ClOrdId {0}", cxlOrderReq.OrigClOrderId), Constants.MessageType.Information);

                CancelOrderWrapper cxlOrderReqWrapper = new CancelOrderWrapper(new Order() { OrigClOrdId = cxlOrderReq.OrigClOrderId, ClOrdId = cxlOrderReq.ClOrderId, Security = new Security() { Symbol = "?" } });
                
                CMState resp = OnMessageReceived(cxlOrderReqWrapper);
                
                if (resp.Success)
                {
                    CancelOrderAck ackMsg = new CancelOrderAck()
                    {
                        Msg = "CancelOrderAck",
                        Success = true,
                    };

                    DoSend<CancelOrderAck>(socket, ackMsg);

                    DoLog(string.Format("Cancel Order Req for ClOrdId {0}  successfully processed", cxlOrderReq.OrigClOrderId), Constants.MessageType.Information);
                }
                else
                    throw resp.Exception;

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR for Cancel Order Req Req for ClOrdId {0}. Error:{1}", cxlOrderReq.OrigClOrderId,ex.Message), Constants.MessageType.Error);

                CancelOrderAck ackMsg = new CancelOrderAck()
                {
                    Msg = "CancelOrderAck",
                    Success = false,
                    Error = ex.Message

                };
            }
            
        }

        protected void ProcessRouteOrderReq(IWebSocketConnection socket, string m)
        {
            NewOrderReq newOrderReq = JsonConvert.DeserializeObject<NewOrderReq>(m);
            try
            {
                
                DoLog(string.Format("Incoming Route Order Req for Symbol {0} and Side {1} Type {2} and Price {3}",
                        newOrderReq.Symbol, newOrderReq.Side,newOrderReq.Type,newOrderReq.Price), Constants.MessageType.Information);

                OrderConverter converter= new OrderConverter();;

                Order newOrder  = converter.ConvertNewOrder(newOrderReq);
                DoLog(string.Format("DBX-Converter Order: Type={0} Price={1}",newOrder.OrdType,
                    newOrder.Price.HasValue?newOrder.Price.Value.ToString():"null."),Constants.MessageType.Information);

                NewOrderWrapper newOrderWrapper = new NewOrderWrapper(newOrder, new Configuration());
                
                CMState resp = OnMessageReceived(newOrderWrapper);
                
                if (resp.Success)
                {
                    NewOrderAck ackMsg = new NewOrderAck()
                    {
                        ReqId = newOrderReq.ReqId,
                        UUID = newOrderReq.UUID,
                        Success = true,
                    };

                    DoSend<NewOrderAck>(socket, ackMsg);

                    DoLog(string.Format("Route Order Req for symbol {0} qty {1} side {2} successfully processed", newOrderReq.Symbol, newOrderReq.Qty, newOrderReq.Side), Constants.MessageType.Information);
                }
                else
                    throw resp.Exception;

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR for Incoming Route Position Req for symbol {0} qty {1} side {2}. Error:{3}", newOrderReq.Symbol, newOrderReq.Qty, newOrderReq.Side,ex.Message), Constants.MessageType.Error);

                NewOrderAck ackMsg = new NewOrderAck()
                {
                    ReqId = newOrderReq.ReqId,
                    Success = false,
                    UUID = newOrderReq.UUID,
                    Error = ex.Message

                };
            }
            
        }
        
        protected void ProcessSubscriptions(IWebSocketConnection socket, string m)
        {
            WebSocketSubscribeMessage subscrMsg = JsonConvert.DeserializeObject<WebSocketSubscribeMessage>(m);

            DoLog(string.Format("Incoming subscription for service {0} - ServiceKey:{1}", subscrMsg.Service, subscrMsg.ServiceKey), Constants.MessageType.Information);

            if (subscrMsg.SubscriptionType == WebSocketSubscribeMessage._SUSBSCRIPTION_TYPE_SUBSCRIBE)
            {
                try
                {


                    if (subscrMsg.Service == _ORDER_BOOK_SERVICE)
                    {
                        ProcessOrderBookRequest(socket, subscrMsg);
                    }
                    else if (subscrMsg.Service == _MARKET_DATA_SERVICE)
                    {
                        ProcessMarketDataRequest(socket, subscrMsg);
                    }
                    else if (subscrMsg.Service == _CANDLEBAR_SERVICE)
                    {
                        ProcessCandlebarRequest(socket, subscrMsg);
                    }
                    else
                    {
                        throw new Exception(string.Format("Subscription method not available:{0}", subscrMsg.Service));
                    }

                }
                catch (Exception e)
                {
                    ProcessSubscriptionResponse(socket, subscrMsg.Service, subscrMsg.ServiceKey, subscrMsg.UUID,false, e.Message);
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
                                //DoLog(string.Format("DB-@DoSend:{0}",ConnectionSocket.ConnectionInfo.ClientIpAddress),Constants.MessageType.Information);
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
                else if (wsResp.Msg == "NewOrderReq")
                {
                    ProcessRouteOrderReq(socket, m);
                }
                else if (wsResp.Msg == "CancelOrderReq")
                {
                    ProcessCancelOrderReq(socket, m);
                }
                else if (wsResp.Msg == "CancelAllReq")
                {
                    ProcessCancelAllReq(socket, m);
                }
                else if (wsResp.Msg == "UpdOrderReq")
                {
                    ProcessUpdateOrderReq(socket, m);
                }
                else if (wsResp.Msg == "OrderMassStatusRequest")
                {
                    ProcessOrderMassStatusRequest(socket, m);
                }
                else if (wsResp.Msg == "HistoricalPricesRequest")
                {
                    ProcessHistoricalPricesRequest(socket, m);
                }
                else if (wsResp.Msg == "SecurityListRequest")
                {
                    ProcessSecurityListRequest(socket, m);
                }
                else
                {
                    throw new Exception(string.Format("Not recognized messag {0}", wsResp.Msg));
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
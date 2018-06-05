using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bloomberg.Common;
using zHFT.OrderRouters.Bloomberg.Common.DTOs;
using zHFT.OrderRouters.Bloomberg.Common.Wrappers;

namespace zHFT.OrderRouters.Bloomberg
{
    public class OrderRouter : OrderRouterBase
    {

        #region Private Static Consts

        private int _MAX_NEW_ORDER_QUEUE_EVAL_MESSAGE = 20;

        private int _SUBSCRIPTION_MESSAGES_REFRESH_PERIOD = 1000;//miliseconds

        private string _ORD_STATUS_INIT_PAINT = "4";
        private string _ORD_STATUS_NEW_ORDER = "6";
        private string _ORD_STATUS_UPD_ORDER = "7";
        private string _ORD_STATUS_DELETE_ORDER = "8";


        #endregion

        #region Protected Attributes

        protected Common.Configuration.Configuration BloombergConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected Dictionary<string, OrderDTO> OrderList { get; set; }

        protected Dictionary<string, string> OrderIdsMapper { get; set; }

        protected Dictionary<long, List<Message>> ResponseMessages { get; set; }

        protected Dictionary<long, List<Message>> SuscriptionMessages { get; set; }

        //protected Dictionary<int, Contract> ContractList { get; set; }

        public static object tMessageDictionariesLock { get; set; }

        public static object tOrderDictionariesLock { get; set; }


        protected Thread EventsThread { get; set; }

        protected Thread SubscriptionThread { get; set; }

        protected Thread HandleSubscriptionThread { get; set; }


        #endregion

        #region Private Methods

        private void LoadSession()
        {
            DoLog(string.Format("Iniciando conexión con Bloomberg: {0}:{1}", BloombergConfiguration.IP, BloombergConfiguration.Port), Main.Common.Util.Constants.MessageType.Information);

            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerHost = BloombergConfiguration.IP; // default value
            sessionOptions.ServerPort = BloombergConfiguration.Port; // default value

            session = new Session(sessionOptions);

            if (!session.Start())
            {
                throw new Exception(string.Format("Could not start session on host {0}:{1}", BloombergConfiguration.IP, BloombergConfiguration.Port));
            }
            else
                DoLog(string.Format("Conectado exitosamente con Bloomberg on host {0}:{1}", BloombergConfiguration.IP, BloombergConfiguration.Port),Main.Common.Util.Constants.MessageType.Information);

            if (!session.OpenService(BloombergConfiguration.EMSX_Environment))
            {
                string error = string.Format("Could not open service {0}:", BloombergConfiguration.EMSX_Environment);
                DoLog(error, Main.Common.Util.Constants.MessageType.Error);
                throw new Exception(error);
            }
        }

        private void DoRejectOrder(object param)
        {

            try
            {
                OrderDTO order = (OrderDTO)((object[])param)[0];
                string errorMsg = (string)((object[])param)[1];

                ManualExecutionReportWrapper wrapper = new ManualExecutionReportWrapper(order,
                                                                            OrdStatus.Rejected,
                                                                            ExecType.Rejected,
                                                                            errorMsg,
                                                                            BloombergConfiguration);
                OnMessageRcv(wrapper);
            }
            catch (Exception ex)
            {
                DoLog("Critic error rejecting order :" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
            }
        }

        private void DoSendExecutionReport(object param)
        {
            try
            {
                Wrapper wrapper = (Wrapper)param;
                OnMessageRcv(wrapper);
            }
            catch (Exception ex)
            {
                DoLog("Critic error sending execution report :" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
            }
        }

        private void RejectOrder(OrderDTO order, string errorMsg)
        {
            Thread rejectOrderThread = new Thread(DoRejectOrder);
            rejectOrderThread.Start(new object[] { order, errorMsg });
        }

        private void SendExecutionReport(Wrapper wrapper)
        {
            Thread executionReportThread = new Thread(DoSendExecutionReport);
            executionReportThread.Start(wrapper);
        }

        private void UpdateDictionary(Event nevent,Dictionary<long, List<Message>> dictionary)
        {
            IEnumerable<Message> messages = nevent.GetMessages();

            foreach (Message message in messages)
            {
                lock (tMessageDictionariesLock)
                {
                    if (dictionary.ContainsKey(message.CorrelationID.Value))
                    {
                        List<Message> reqMessages = dictionary[message.CorrelationID.Value];
                        reqMessages.Add(message);
                    }
                    else
                    {
                        List<Message> reqMessages = new List<Message>();
                        reqMessages.Add(message);
                        dictionary.Add(message.CorrelationID.Value, reqMessages);
                    }
                }
            }
        }

        private void CleanDictionaries(string EMSX_SEQUENCE)
        {
            try
            {
                OrderList.Remove(EMSX_SEQUENCE);

                List<string> keysToRemove = new List<string>();
                foreach (string key in OrderIdsMapper.Keys)
                {
                    string localEMSX_SEQUENCE = OrderIdsMapper[key];
                    if (localEMSX_SEQUENCE == EMSX_SEQUENCE)
                        keysToRemove.Add(key);
                }

                keysToRemove.ForEach(x => OrderIdsMapper.Remove(x));
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critica Error Cleaning Dictioaries!:{1}", BloombergConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        private void DoHandleSubscriptions(object param)
        {
            bool continueToLoop = true;

            while (continueToLoop)
            {
               List<Message> toRemove = new List<Message>();
                lock (tMessageDictionariesLock)
                {
                    foreach (List<Message> messages in SuscriptionMessages.Values)
                    {
                        try
                        {
                            
                            foreach (Message message in messages)
                            {
                                if (message.MessageType.ToString() == "OrderRouteFields")
                                {
                                    //Este tiene que ser un mensaje de suscripción
                                    string status = message.AsElement.GetElementAsString("EVENT_STATUS");
                                    string EMSX_SEQUENCE = message.GetElementAsString("EMSX_SEQUENCE");


                                    if (OrderList.Keys.Contains(EMSX_SEQUENCE))
                                    {
                                        OrderDTO order = OrderList[EMSX_SEQUENCE];

                                        if (status == _ORD_STATUS_INIT_PAINT)//Mensaje Inicial
                                        {
                                            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, message, BloombergConfiguration);
                                            SendExecutionReport(wrapper);//Publicamos la novedad
                                        }
                                        else if (status == _ORD_STATUS_NEW_ORDER)//New Order
                                        {
                                            
                                            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, message, BloombergConfiguration);
                                            SendExecutionReport(wrapper);//Publicamos la novedad
                                        }
                                        else if (status == _ORD_STATUS_UPD_ORDER)//Update Order
                                        {
                                            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, message, BloombergConfiguration);
                                            SendExecutionReport(wrapper);//Publicamos la novedad
                                        }
                                        else if (status == _ORD_STATUS_DELETE_ORDER)//Delete Order
                                        {
                                            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, message, BloombergConfiguration);
                                            SendExecutionReport(wrapper);//Publicamos la novedad

                                            lock (tOrderDictionariesLock)
                                            {
                                                CleanDictionaries(EMSX_SEQUENCE);
                                            }
                                        }
                                    }
                                    //Aca recibimos datos de una orden que no estamos procesando

                                    toRemove.Add(message);//Los procesamos una vez y listo
                                }
                                else
                                    toRemove.Add(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            DoLog(string.Format("@{0}:Critica Error processing Bloomberg Subscription Message!:{1}", BloombergConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                        }
                        finally
                        {
                            toRemove.ForEach(x => messages.Remove(x));
                            toRemove.Clear();
                        }
                    }
                }

                Thread.Sleep(_SUBSCRIPTION_MESSAGES_REFRESH_PERIOD);
            }
        }

        private void DoHandleEvents(object param)
        {
          
            bool active=true;
            while (active)
            {
                try
                {
                    Event nevent = session.NextEvent();

                    switch (nevent.Type)
                    {

                        case Event.EventType.RESPONSE: // final event
                            UpdateDictionary(nevent,ResponseMessages);
                            break;
                        case Event.EventType.PARTIAL_RESPONSE:
                            UpdateDictionary(nevent, ResponseMessages);
                            break;
                        case Event.EventType.SUBSCRIPTION_DATA:
                            UpdateDictionary(nevent, SuscriptionMessages);
                            break;
                        case Event.EventType.SUBSCRIPTION_STATUS:
                            UpdateDictionary(nevent, SuscriptionMessages);
                            break;
                        default:
                                
                            break;
                        
                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Error processing Bloomberg Events!:{1}", BloombergConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        private string GetOrderSuscriptionFields()
        {
            string strOrderSubscriptionFields = "//blp/emapisvc_beta/order?fields=";
            strOrderSubscriptionFields += "API_SEQ_NUM,";
            strOrderSubscriptionFields += "EMSX_ACCOUNT,";
            strOrderSubscriptionFields += "EMSX_AMOUNT,";
            strOrderSubscriptionFields += "EMSX_ARRIVAL_PRICE,";
            strOrderSubscriptionFields += "EMSX_ASSET_CLASS,";
            strOrderSubscriptionFields += "EMSX_ASSIGNED_TRADER,";
            strOrderSubscriptionFields += "EMSX_AVG_PRICE,";
            strOrderSubscriptionFields += "EMSX_BASKET_NAME,";
            strOrderSubscriptionFields += "EMSX_BASKET_NUM,";
            strOrderSubscriptionFields += "EMSX_BROKER,";
            strOrderSubscriptionFields += "EMSX_BROKER_COMM,";
            strOrderSubscriptionFields += "EMSX_BSE_AVG_PRICE,";
            strOrderSubscriptionFields += "EMSX_BSE_FILLED,";
            strOrderSubscriptionFields += "EMSX_CFD_FLAG,";
            strOrderSubscriptionFields += "EMSX_COMM_DIFF_FLAG,";
            strOrderSubscriptionFields += "EMSX_COMM_RATE,";
            strOrderSubscriptionFields += "EMSX_STATUS,"; 
            strOrderSubscriptionFields += "EMSX_DATE,";
            strOrderSubscriptionFields += "EMSX_FILLED,";
            strOrderSubscriptionFields += "EMSX_DAY_AVG_PRICE";

            return strOrderSubscriptionFields;
        
        }

        private string GetRouteSuscriptionFields()
        {
            string strRouteSubscriptionFields = "//blp/emapisvc_beta/route?fields=";
            strRouteSubscriptionFields += "API_SEQ_NUM,";
            strRouteSubscriptionFields += "EMSX_AMOUNT,";
            strRouteSubscriptionFields += "EMSX_AVG_PRICE,";
            strRouteSubscriptionFields += "EMSX_LAST_SHARES,";
            strRouteSubscriptionFields += "EMSX_LAST_PRICE,";
            strRouteSubscriptionFields += "EMSX_BROKER,";
            strRouteSubscriptionFields += "EMSX_BROKER_COMM,";
            strRouteSubscriptionFields += "EMSX_BSE_AVG_PRICE,";
            strRouteSubscriptionFields += "EMSX_AMOUNT,";
            strRouteSubscriptionFields += "EMSX_BSE_FILLED,";
            strRouteSubscriptionFields += "EMSX_FILLED,";
            strRouteSubscriptionFields += "EMSX_CLEARING_ACCOUNT,";
            strRouteSubscriptionFields += "EMSX_STATUS,"; 
            strRouteSubscriptionFields += "EMSX_CLEARING_FIRM";

            return strRouteSubscriptionFields;
        }

        private void DoSubscribe(object param)
        {
            try
            {
                List<Subscription> subscriptions = new List<Subscription>();

                //Nos suscribimos a todos los eventos de ORDERS y los atributos especificados abajo
                string strOrderSubscriptionFields = GetOrderSuscriptionFields();
                string strRouteSubscriptionFields = GetRouteSuscriptionFields();

                Subscription orderSubscription = new Subscription(strOrderSubscriptionFields);
                Subscription routeSubscription = new Subscription(strRouteSubscriptionFields);

                subscriptions.Add(orderSubscription);
                subscriptions.Add(routeSubscription);

                session.Subscribe(subscriptions);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Critical Error subscribing to Bloomberg!:{1}", BloombergConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return;
            }
        }

        #region New Order Methods

        private CMState ProcessNewOrderMessage(OrderDTO order,Message message, ref string EMSX_SEQUENCE)
        {
            Element element = message.AsElement;
            CMState state = null;

            if (element.HasElement("ERROR_CODE"))
            {
                RejectOrder(order, element.GetElementAsString("ERROR_MESSAGE"));
                state = CMState.BuildFail(new Exception(element.GetElementAsString("ERROR_MESSAGE")));
            }
            else
            {
                EMSX_SEQUENCE = message.AsElement.GetElementAsString("EMSX_SEQUENCE");

                if (EMSX_SEQUENCE != null)
                {
                    state = CMState.BuildSuccess();
                }
                else
                {
                    string error = string.Format("Error desconocido recuperando el número de secuencia EMSX para la orden del activo {0}", order.Ticker);
                    RejectOrder(order, error);
                    state = CMState.BuildFail(new Exception(error));
                }
            }

            return state;
        
        }

        private CMState HandleNewOrderResponseEvent(OrderDTO order, CorrelationID correlationId, ref string EMSX_SEQUENCE)
        {
            bool continueToLoop = true;

            int i = 0;

            while (continueToLoop)
            {
                lock (tMessageDictionariesLock)
                {
                    if (ResponseMessages.ContainsKey(correlationId.Value))
                    {
                        List<Message> messages = ResponseMessages[correlationId.Value];
                        List<Message> toRemove = new List<Message>();

                        bool processed = false;
                        CMState state = null;

                        if (messages == null)
                            continue;

                        foreach (Message message in messages)
                        {
                            //De todos los mensajes si encontramos uno de CrateOrder de la orden búscada
                            
                            if (message.CorrelationID == correlationId && message.MessageType.ToString() == "CreateOrderAndRouteEx")
                            //if (message.CorrelationID==correlationId && message.MessageType.ToString() == "CreateOrder")
                            {
                                try
                                {
                                    state = ProcessNewOrderMessage(order, message, ref EMSX_SEQUENCE);
                                    processed = true;
                                    toRemove.Add(message);//Ya lo procesamos, lo marcamos para eliminar
                                }
                                catch (Exception ex)
                                {
                                    RejectOrder(order, ex.Message);
                                    state = CMState.BuildFail(ex);
                                    processed = true;
                                    toRemove.Add(message);//Ya lo procesamos, lo marcamos para eliminar
                                }
                            }
                        }

                        if (processed)
                        {
                            toRemove.ForEach(x => messages.Remove(x));//Eliminamos los mensajes ya procesados de creación de ordenes
                            return state;
                        }
                    }
                }

                i++;

                if (i >= _MAX_NEW_ORDER_QUEUE_EVAL_MESSAGE)
                {
                    string error = string.Format("Timeout requesting for new order EMSX_SEQUENCE", order.Ticker);
                    RejectOrder(order, error);
                    return CMState.BuildFail(new Exception(error));
                }
                Thread.Sleep(500);
            }
            
            return CMState.BuildSuccess();
        }

        private OrderDTO LoadOrder(Wrapper wrapper)
        {
            OrderDTO order = new OrderDTO();

            order.ClOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

            order.Ticker = (string)wrapper.GetField(OrderFields.Symbol);
            order.OrderQty = Convert.ToInt32(wrapper.GetField(OrderFields.OrderQty));
            order.Price = Convert.ToDouble(wrapper.GetField(OrderFields.Price));
            order.OrdType = OrdType.Limit;
            order.TimeInForce = TimeInForce.Day;
            order.Side = (Side)wrapper.GetField(OrderFields.Side);
            order.OrderId = NextOrderId;
            order.Exchange = BloombergConfiguration.Exchange;
            order.Broker = BloombergConfiguration.Broker;

            SecurityType type = (SecurityType)wrapper.GetField(OrderFields.SecurityType);
            order.SecurityType = type;

            NextOrderId++;

            return order;
        }

        private Request BuildNewOrderRequest(Service service,OrderDTO order)
        {
            //Request request = service.CreateRequest("CreateOrder");
            Request request = service.CreateRequest("CreateOrderAndRouteEx");

            //The fields below are mandatory
            request.Set("EMSX_TICKER", order.GetFullBloombergSymbol());
            request.Set("EMSX_AMOUNT", order.OrderQty);
            request.Set("EMSX_LIMIT_PRICE", order.Price);
            request.Set("EMSX_ORDER_TYPE", order.GetOrdType());//ORDENES FIJO DE TIPO LIMIT
            request.Set("EMSX_TIF", order.GetTimeInForce());
            request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            request.Set("EMSX_SIDE", order.GetSide());
            request.Set("EMSX_BROKER", order.Broker);

            //request.Set("EMSX_TICKER", "IBM US Equity");
            //request.Set("EMSX_AMOUNT", 1000);
            //request.Set("EMSX_ORDER_TYPE", "MKT");
            //request.Set("EMSX_TIF", "DAY");
            //request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            //request.Set("EMSX_SIDE", "BUY");

            return request;
        }

        #endregion

        #region Delete/Cancel Order Methods

        private CMState DoCancelOrder(string EMSX_SEQUENCE)
        {
            OrderDTO order = OrderList[EMSX_SEQUENCE];

            Service service = session.GetService("//blp/emapisvc_beta");
            Request request = service.CreateRequest("CancelRoute");

            Element routes = request.GetElement("ROUTES"); //Note, the case is important.
            Element route = routes.AppendElement(); // Multiple routes can be cancelled in a single request
            route.GetElement("EMSX_SEQUENCE").SetValue(EMSX_SEQUENCE);
            route.GetElement("EMSX_ROUTE_ID").SetValue(1);

            CorrelationID requestID = new CorrelationID(order.OrderId);
            session.SendRequest(request, requestID);

            //CMState state = HandleDeleteOrderResponseEvent(order, requestID, EMSX_SEQUENCE);
            CMState state = CMState.BuildSuccess();

            return state;

        }

        #endregion

        #region UpdateOrder Methods

        private Request BuildUpdateOrderRequest(Service service, OrderDTO order)
        {
            //Request request = service.CreateRequest("ModifyOrder");
            Request request = service.CreateRequest("ModifyOrderEx");

            //The fields below are mandatory
            request.Set("EMSX_TICKER", order.GetFullBloombergSymbol());
            request.Set("EMSX_SEQUENCE", order.MarketOrderId);
            request.Set("EMSX_AMOUNT", order.OrderQty);
            request.Set("EMSX_ORDER_TYPE", order.GetOrdType());//ORDENES FIJO DE TIPO LIMIT
            request.Set("EMSX_LIMIT_PRICE", order.Price);
            request.Set("EMSX_TIF", order.GetTimeInForce());
            request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            //request.Set("EMSX_BROKER", order.Broker); Activar cuando este apuntando al mercado

            return request;
        }

        private CMState DoUpdateOrder(OrderDTO order)
        {
            Service service = session.GetService("//blp/emapisvc_beta");
            Request request = BuildUpdateOrderRequest(service, order);

            CorrelationID requestID = new CorrelationID(order.OrderId);
            session.SendRequest(request, requestID);

            //TO DO : Eval la respuesta de la actualización
            CMState state = CMState.BuildSuccess();

            return state;

        }

        #endregion

        #region Action Methods

        protected CMState RouteNewOrder(Wrapper wrapper)
        {
            Service service = session.GetService(BloombergConfiguration.EMSX_Environment);

            OrderDTO order=null;
            Request request = null;
            CorrelationID requestID = null;
            string EMSX_SEQUENCE = null;

            order = LoadOrder(wrapper);

            request = BuildNewOrderRequest(service, order);

            requestID = new CorrelationID(order.OrderId);

            CorrelationID resp = session.SendRequest(request, requestID);

            CMState state = HandleNewOrderResponseEvent(order, requestID, ref EMSX_SEQUENCE);

            if(state.Success)
            {
                lock (tOrderDictionariesLock)
                {
                    order.MarketOrderId = EMSX_SEQUENCE;
                    OrderList.Add(EMSX_SEQUENCE, order);
                    OrderIdsMapper.Add(order.ClOrderId, EMSX_SEQUENCE);
                }
            
                DoLog(string.Format("Routing Order Id {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);
                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");
            }

            return state;
        }

        protected CMState RouteUpdateOrder(Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
                //New order id
                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

                lock (tOrderDictionariesLock)
                {
                    if (OrderIdsMapper.ContainsKey(origClOrderId))
                    {
                        string EMSX_SEQUENCE = OrderIdsMapper[origClOrderId];

                        OrderDTO order =  OrderList[EMSX_SEQUENCE];

                        order.Price = (double)wrapper.GetField(OrderFields.Price);

                        OrderIdsMapper.Add(clOrderId, EMSX_SEQUENCE);

                        return DoUpdateOrder(order);

                    }
                    else
                    {
                        string error = string.Format("@{0}:Could not find an order for ClOrdId {1}", BloombergConfiguration.Name, clOrderId);
                        DoLog(error, Main.Common.Util.Constants.MessageType.Error);
                        return CMState.BuildFail(new Exception(string.Format("Could not find an order for ClOrdId {0}", clOrderId)));
                    }
                }
            }
            catch (Exception ex)
            {
                string error = string.Format("@{0}:Critical error updating order:{1}", BloombergConfiguration.Name,ex.Message);
                DoLog(error, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(new Exception(error));
            }
        }

        protected CMState CancelAllActiveOrders()
        {
            try
            {
                lock (tOrderDictionariesLock)
                {
                    foreach (string EMSX_SEQUENCE in OrderList.Keys)
                    {
                        DoCancelOrder(EMSX_SEQUENCE);
                        Thread.Sleep(100);
                    }
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                string error = string.Format("@{0}:Critical error cancelling orders:{1}", BloombergConfiguration.Name,ex.Message);
                DoLog(error, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(new Exception(error));
            }
        }

        protected CMState CancelOrder(Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                    throw new Exception("Could not find OrigClOrdID for order updated");

                if (wrapper.GetField(OrderFields.ClOrdID) == null)
                    throw new Exception("Could not find ClOrdId for new order");

                string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
                //New order id
                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

                lock (tOrderDictionariesLock)
                {
                    if (OrderIdsMapper.ContainsKey(origClOrderId))
                    {
                        string EMSX_SEQUENCE = OrderIdsMapper[origClOrderId];

                        return DoCancelOrder(EMSX_SEQUENCE);

                    }
                    else
                    {
                        string error = string.Format("@{0}:Could not find an order for ClOrdId {1}", BloombergConfiguration.Name, clOrderId);
                        DoLog(error, Main.Common.Util.Constants.MessageType.Error);
                        return CMState.BuildFail(new Exception(string.Format("Could not find an order for ClOrdId {0}", clOrderId)));
                    }
                }
            }
            catch(Exception ex)
            {
                string error = string.Format("@{0}:Critical error cancelling order: {1}",BloombergConfiguration.Name,ex.Message);
                DoLog(error,Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(new Exception(error));
            }
        }

        #endregion

        #endregion

        #region Potected Methods

        public override Main.Common.DTO.CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("Routing with Bloomberg to market for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    return RouteNewOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("Updating order with Bloomberg  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    return RouteUpdateOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("Canceling order with Bloomberg  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    return CancelOrder(wrapper);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders @ Bloomberg", BloombergConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                    return CancelAllActiveOrders();
                }
                else
                {
                    DoLog(string.Format("Could not process order routing for action {0} with Bloomberg:", wrapper.GetAction().ToString()),
                          Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("Could not process order routing for action {0} with IB:", wrapper.GetAction().ToString())));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog("Error routing order to market using Bloomberg:" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public override bool Initialize(Main.Common.Interfaces.OnMessageReceived pOnMessageRcv, Main.Common.Interfaces.OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tMessageDictionariesLock = new object();
                    tOrderDictionariesLock = new object();

                    LoadSession();

                    NextOrderId = BloombergConfiguration.InitialOrderId;

                    ResponseMessages = new Dictionary<long, List<Message>>();
                    SuscriptionMessages = new Dictionary<long, List<Message>>();

                    OrderList = new Dictionary<string, OrderDTO>();
                    OrderIdsMapper = new Dictionary<string, string>();

                    EventsThread = new Thread(DoHandleEvents);
                    EventsThread.Start();

                    SubscriptionThread = new Thread(DoSubscribe);
                    SubscriptionThread.Start();

                    HandleSubscriptionThread = new Thread(DoHandleSubscriptions);
                    HandleSubscriptionThread.Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected override Main.Common.DTO.CMState ProcessIncoming(Main.Common.Wrappers.Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No incoming module set for Bloomberg order router!"));
 
        }

        protected override Main.Common.DTO.CMState ProcessOutgoing(Main.Common.Wrappers.Wrapper wrapper)
        {
            //Este Communication Module no tiene modulos de Incoming o Outgoing
            return CMState.BuildFail(new Exception("No outgoing module set for Bloomberg order router!"));

        }
       
        #endregion
    }
}

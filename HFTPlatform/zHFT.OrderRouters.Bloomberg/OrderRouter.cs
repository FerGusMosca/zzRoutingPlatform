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

        #region Protected Attributes

        protected Common.Configuration.Configuration BloombergConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        //protected Dictionary<int, Order> OrderList { get; set; }

        protected Dictionary<string, int> OrderIdsMapper { get; set; }

        //protected Dictionary<int, Contract> ContractList { get; set; }

        public static object tLock { get; set; }


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
        }

        private OrderDTO LoadOrder(Wrapper wrapper)
        {
            OrderDTO order = new OrderDTO();

            order.ClOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

            order.Ticker = (string)wrapper.GetField(OrderFields.Symbol); 
            order.OrderQty=Convert.ToInt32(wrapper.GetField(OrderFields.OrderQty));
            order.Price = Convert.ToDouble(wrapper.GetField(OrderFields.Price));
            order.OrdType=OrdType.Limit;
            order.TimeInForce=TimeInForce.Day;
            order.Side=(Side)wrapper.GetField(OrderFields.Side);
            order.OrderId=NextOrderId;
            order.Exchange=BloombergConfiguration.Exchange;
            order.Broker = BloombergConfiguration.Broker;

             SecurityType type = (SecurityType) wrapper.GetField(OrderFields.SecurityType);
             order.SecurityType = type;

            NextOrderId++;

            return order;
        }

        private void DoRejectOrder(object param)
        {

            try
            {
                OrderDTO order = (OrderDTO)((object[])param)[0];
                string errorMsg = (string)((object[])param)[1];

                ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order,
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

        private void RejectOrder(OrderDTO order, string errorMsg)
        {
            Thread rejectOrderThread = new Thread(DoRejectOrder);
            rejectOrderThread.Start(new object[] { order, errorMsg });
        }

        private CMState HandleResponseEvent(OrderDTO order,Event nevent, CorrelationID correlationId)
        {
            DoLog("EventType =" + nevent.Type,Main.Common.Util.Constants.MessageType.Information);
            IEnumerable<Message> messages = nevent.GetMessages();

            foreach (Message message in messages)
            {
                if (message.CorrelationID == correlationId)
                {
                    DoLog(string.Format("CorrelationID {0}", message.CorrelationID), Main.Common.Util.Constants.MessageType.Information);
                    DoLog(string.Format("Type {0}", message.MessageType), Main.Common.Util.Constants.MessageType.Information);

                    Element element = message.AsElement;

                    if (element.HasElement("ERROR_CODE"))
                    {
                        RejectOrder(order, element.GetElementAsString("ERROR_MESSAGE"));
                        return CMState.BuildFail(new Exception(element.GetElementAsString("ERROR_MESSAGE")));
                    }

                    DoLog(message.AsElement.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    return CMState.BuildSuccess();
                }
            }
            return CMState.BuildSuccess();
        }

        private CMState HandleResponse(OrderDTO order ,CorrelationID correlationID)
        {

            CMState state = null;
            try
            {
                bool continueToLoop = true;
                while (continueToLoop)
                {

                    Event nevent = session.NextEvent();
                    switch (nevent.Type)
                    {

                        case Event.EventType.RESPONSE: // final event
                            state=HandleResponseEvent(order,nevent,correlationID);
                            continueToLoop = false; // fall through
                            break;
                        case Event.EventType.PARTIAL_RESPONSE:
                            state = HandleResponseEvent(order,nevent,correlationID);
                            break;
                        default:
                            //Console.WriteLine(string.Format("Evento no reconocido {0}", nevent.Type));
                            IEnumerable<Message> messages = nevent.GetMessages();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
               RejectOrder(order, ex.Message);
               return CMState.BuildFail( ex);
            }
            return state;
        }



        protected CMState RouteNewOrder(Wrapper wrapper)
        {
            session.OpenService(BloombergConfiguration.EMSX_Environment);
            Service service = session.GetService(BloombergConfiguration.EMSX_Environment);
            Request request = service.CreateRequest("CreateOrder");

            OrderDTO order=null;
            CorrelationID requestID = null;

            lock (tLock)
            {
                order = LoadOrder(wrapper);

                //Extraemos los campos
                Side side = (Side)wrapper.GetField(OrderFields.Side);

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
                requestID = new CorrelationID(order.OrderId);

                CorrelationID resp = session.SendRequest(request, requestID);

                OrderIdsMapper.Add(order.ClOrderId, order.OrderId);
            }
            
            DoLog(string.Format("Routing Order Id {0}", order.OrderId), Main.Common.Util.Constants.MessageType.Information);
            if (wrapper.GetField(OrderFields.ClOrdID) == null)
                throw new Exception("Could not find ClOrdId for new order");

            
            //OrderList.Add(order.OrderId, order);
            //ContractList.Add(order.OrderId, contract);

            return HandleResponse(order,requestID);
        }

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
                    //UpdateOrder(wrapper, false);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("Canceling order with Bloomberg  for symbol {0}", wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    //UpdateOrder(wrapper, true);
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("@{0}:Cancelling all active orders @ Bloomberg", BloombergConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);
                    //CancelAllOrders();
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
                    tLock = new object();

                    LoadSession();

                    NextOrderId = BloombergConfiguration.InitialOrderId;
                    
                    //OrderList = new Dictionary<int, Order>();
                    OrderIdsMapper = new Dictionary<string, int>();
                    //ContractList = new Dictionary<int, Contract>();

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

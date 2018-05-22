using Bloomberglp.Blpapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorld.Bloomberg.Test
{
    class Program
    {
        #region Private Static Attributes

        private static int Sequence=1;

        #endregion

        private static void handleResponseEvent(Event nevent)
        {
             Console.WriteLine("EventType =" + nevent.Type);
             IEnumerable<Message> messages  = nevent.GetMessages();


            foreach(Message message in  messages)
            {
                Console.WriteLine(string.Format("CorrelationID {0}",message.CorrelationID));
                Console.WriteLine(string.Format("Type {0}",message.MessageType));

                Console.WriteLine(message.AsElement);
            
            }
        }

        private static void DeleteActiveOrder(Event nevent, Session session)
        {
            IEnumerable<Message> messages = nevent.GetMessages();
            foreach (Message message in messages)
            {

                string MSG_SUB_TYPE = message.AsElement.GetElementAsString("MSG_SUB_TYPE");
                string EVENT_STATUS = message.AsElement.GetElementAsString("EVENT_STATUS");
                string EMSX_SEQUENCE = message.AsElement.GetElementAsString("EMSX_SEQUENCE");

                if (EMSX_SEQUENCE != null)
                {
                    DeleteLight(session, EMSX_SEQUENCE, Sequence);
                    Sequence++;
                }

                //if (EVENT_STATUS == "4")
                //{
                //    DeleteLight(session, EMSX_SEQUENCE, Sequence);

                //    Sequence++;
                //}
                //else
                //{ 
                
                
                
                //}

                Console.WriteLine(string.Format("Eliminando orden de EMSX_SEQUENCE {0}", EMSX_SEQUENCE));
            }
        }

        private static string  handleNewOrderEvent(Event nevent)
        {
            Console.WriteLine("EventType =" + nevent.Type);
            IEnumerable<Message> messages = nevent.GetMessages();

            string EMSX_SEQUENCE = null;
            foreach (Message message in messages)
            {
                Console.WriteLine(string.Format("CorrelationID {0}", message.CorrelationID));
                Console.WriteLine(string.Format("Type {0}", message.MessageType));

                Console.WriteLine(message.AsElement);

                EMSX_SEQUENCE=message.AsElement.GetElementAsString("EMSX_SEQUENCE");

            }
            return EMSX_SEQUENCE;

        }

        private static void AuthUser(Session session)
        {
            string authsvc = "//blp/apiauth";
            session.OpenService(authsvc);


            Service authService = session.GetService(authsvc);

            Request authReq = authService.CreateAuthorizationRequest();

            authReq.Set("authId", "22748905");
            authReq.Set("ipAddress", "10.12.10.89");

            Identity userIdentity = session.CreateIdentity();
            CorrelationID authRequestID = new CorrelationID();
        
        
            try
            {
                session.SendAuthorizationRequest(authReq, userIdentity, authRequestID);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to send authorization request: " + e.Message);
            }

            bool continueToLoop = true;
            while (continueToLoop)
            {

                Event nevent = session.NextEvent();
                switch (nevent.Type)
                {

                    case Event.EventType.RESPONSE: // final event
                        handleResponseEvent(nevent);
                        continueToLoop = false; // fall through
                        break;
                    case Event.EventType.PARTIAL_RESPONSE:
                        handleResponseEvent(nevent);
                        break;
                    default:
                        Console.WriteLine(string.Format("Evento no reconocido {0}", nevent.Type));
                        IEnumerable<Message> messages = nevent.GetMessages();
                        break;
                }
            }

        }

        #region Routing Methods

        protected static void RouteOrder(Session session)
        {
            //AuthUser(session);

            session.OpenService("//blp/emapisvc_beta");

            Service service = session.GetService("//blp/emapisvc_beta");
            Request request = service.CreateRequest("CreateOrderAndRouteEx");

             //The fields below are mandatory
            request.Set("EMSX_TICKER", "UNH US Equity");
            request.Set("EMSX_AMOUNT", 1000);
            request.Set("EMSX_ORDER_TYPE", "MKT");
            request.Set("EMSX_TIF", "DAY");
            request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            request.Set("EMSX_SIDE", "BUY");
            request.Set("EMSX_BROKER", "EFIX");
            //request.Set("EMSX_TICKER", "IBM US Equity");
            //request.Set("EMSX_AMOUNT", 1000);
            //request.Set("EMSX_ORDER_TYPE", "MKT");
            //request.Set("EMSX_TIF", "DAY");
            //request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            //request.Set("EMSX_SIDE", "BUY");
            //request.Set("EMSX_BROKER", "BB");

            CorrelationID requestID = new CorrelationID(Sequence);
            Sequence++;

            string orderSequence = "";

            // Submit the request
            try
            {
               
               CorrelationID resp =  session.SendRequest(request, requestID);

               bool continueToLoop = true;
               while (continueToLoop)
               {

                   Event nevent = session.NextEvent();
                   switch (nevent.Type)
                   {

                       case Event.EventType.RESPONSE: // final event
                           orderSequence=handleNewOrderEvent(nevent);
                           continueToLoop = false; // fall through
                           break;
                       case Event.EventType.PARTIAL_RESPONSE:
                           orderSequence = handleNewOrderEvent(nevent);
                           break;
                       default:
                           Console.WriteLine(string.Format("Evento no reconocido {0}", nevent.Type));
                           IEnumerable<Message> messages = nevent.GetMessages();


                           break;
                   }
               }

               //DeleteOrder(session, orderSequence);
              

               SubscribeOrder(session, orderSequence);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine("Failed to send the request: " + ex.Message);
            }
        
        
        }

        protected static void DeleteLight(Session session, string EMSX_SEQUENCE,int correlationID)
        {
            Service service = session.GetService("//blp/emapisvc_beta");
            Request request = service.CreateRequest("DeleteOrder");

            request.GetElement("EMSX_SEQUENCE").AppendValue(EMSX_SEQUENCE);

            CorrelationID requestID = new CorrelationID(correlationID);
            session.SendRequest(request, requestID);
        }

        protected static void DeleteOrder(Session session,string EMSX_SEQUENCE)
        {

            Service service = session.GetService("//blp/emapisvc_beta");
            Request request = service.CreateRequest("DeleteOrder");

            request.GetElement("EMSX_SEQUENCE").AppendValue(EMSX_SEQUENCE);

            CorrelationID requestID = new CorrelationID(Sequence);
            Sequence++;

            session.SendRequest(request, requestID);

            try
            {

               bool continueToLoop = true;
               while (continueToLoop)
               {

                   Event nevent = session.NextEvent();
                   switch (nevent.Type)
                   {

                       case Event.EventType.RESPONSE: // final event
                           handleResponseEvent(nevent);
                           //continueToLoop = false; // fall through
                           break;
                       case Event.EventType.PARTIAL_RESPONSE:
                           handleResponseEvent(nevent);
                           break;
                       default:
                           Console.WriteLine(string.Format("Evento no reconocido {0}", nevent.Type));
                           IEnumerable<Message> messages = nevent.GetMessages();


                           break;
                   }
               }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Failed to send the request: " + ex.Message);
            }
        
        }

        protected static void SubscribeOrder(Session session, string EMSX_SEQUENCE)
        {

            List<Subscription> subscriptions = new List<Subscription>();

            string strSubscriptionFields = "//blp/emapisvc_beta/order?fields=";
            strSubscriptionFields += "API_SEQ_NUM,";
            strSubscriptionFields += "EMSX_AMOUNT,";
            strSubscriptionFields += "EMSX_AVG_PRICE,";
            strSubscriptionFields += "EMSX_BROKER,";
            strSubscriptionFields += "EMSX_ASSET_CLASS,";
            strSubscriptionFields += "EMSX_DATE,";
            strSubscriptionFields += "EMSX_SEQUENCE,";
            strSubscriptionFields += "EMSX_SIDE,";
            strSubscriptionFields += "EMSX_TIF,";
            strSubscriptionFields += "EMSX_STATUS,";
            strSubscriptionFields += "EMSX_LIMIT_PRICE,";
            strSubscriptionFields += "EMSX_SEC_NAME,";
            strSubscriptionFields += "EMSX_ORDER_TYPE";

            Subscription subscription = new Subscription(strSubscriptionFields);

            subscriptions.Add(subscription);

            session.Subscribe(subscriptions);

            try
            {

                bool continueToLoop = true;
                while (continueToLoop)
                {

                    Event nevent = session.NextEvent();
                    switch (nevent.Type)
                    {

                        case Event.EventType.SUBSCRIPTION_DATA: // final event
                            handleResponseEvent(nevent);
                            continueToLoop = false; // fall through
                            break;
                        case Event.EventType.SUBSCRIPTION_STATUS: // final event
                            handleResponseEvent(nevent);
                            //continueToLoop = false; // fall through
                            break;
                        default:
                            Console.WriteLine(string.Format("Evento no reconocido {0}", nevent.Type));
                            IEnumerable<Message> messages = nevent.GetMessages();


                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Failed to send the request: " + ex.Message);
            }
        
        }

        protected static void DeleteAllActiveOrders(Session session)
        {
            session.OpenService("//blp/emapisvc_beta");

            //Service service = session.GetService("//blp/emapisvc_beta");

            List<Subscription> subscriptions = new List<Subscription>();
            string strSubscriptionFields = "//blp/emapisvc_beta/order?fields=";
            strSubscriptionFields += "EMSX_SEQUENCE";
        
            Subscription subscription = new Subscription(strSubscriptionFields);
            subscriptions.Add(subscription);
            session.Subscribe(subscriptions);

            try
            {

                bool continueToLoop = true;
                while (continueToLoop)
                {

                    Event nevent = session.NextEvent();
                    switch (nevent.Type)
                    {

                        case Event.EventType.SUBSCRIPTION_DATA: // final event
                            DeleteActiveOrder(nevent,session);
                            //continueToLoop = false; // fall through
                            break;
                        case Event.EventType.SUBSCRIPTION_STATUS: // final event
                            handleResponseEvent(nevent);
                            //continueToLoop = false; // fall through
                            break;
                        default:
                            Console.WriteLine(string.Format("Evento no reconocido {0}", nevent.Type));
                            IEnumerable<Message> messages = nevent.GetMessages();


                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Failed to send the request: " + ex.Message);
            }
        }

        private static void RequestMktDataTest(Session session)
        {
            CorrelationID requestID = new CorrelationID(Sequence);
            Sequence++;
             
             Service refDataSvc = session.GetService("//blp/refdata");
             Request request = refDataSvc.CreateRequest("ReferenceDataRequest");
             request.Append("securities", "DIS US Equity");
             request.Append("fields", "PX_LAST");
             session.SendRequest(request, requestID);
             bool continueToLoop = true;
             while (continueToLoop) {

                 Event nevent = session.NextEvent();
                 switch (nevent.Type) {

                     case Event.EventType.RESPONSE: // final event
                            handleResponseEvent(nevent);
                            continueToLoop = false; // fall through
                            break;
                    case Event.EventType.PARTIAL_RESPONSE: 
                            handleResponseEvent(nevent);
                            break;
                    default:
                            Console.WriteLine(string.Format("Evento no reconocido {0}", nevent.Type));
                            IEnumerable<Message> messages = nevent.GetMessages();


                            break;
                }
            }
        
        }

        #endregion

        static void Main(string[] args)
        {
            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerHost="localhost"; // default value
            sessionOptions.ServerPort=8194; // default value
 
            Session session = new Session(sessionOptions);

           
             if (!session.Start()) {
             Console.WriteLine("Could not start session.");
             
             }
             if (!session.OpenService("//blp/refdata")) {
             Console.WriteLine("Could not open service " +"//blp/refdata");
             
             }

             //Service service = session.GetService("//blp/emapisvc_beta");
             //RequestMktDataTest(session);
             //RouteOrder(session);

             DeleteAllActiveOrders(session);

             //DeleteLight(session, "4317388",3);
             //DeleteLight(session, "4317389", 4);
             //DeleteLight(session, "4317390", 5);
             //DeleteLight(session, "4317391", 6);


             Console.ReadKey();
        }
    }
}

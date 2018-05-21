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

        private static void RouteOrder(Session session)
        {
            //AuthUser(session);

            session.OpenService("//blp/emapisvc_beta");

            Service service = session.GetService("//blp/emapisvc_beta");
            Request request = service.CreateRequest("CreateOrder");

             //The fields below are mandatory
            request.Set("EMSX_TICKER", "AAPL US Equity");
            request.Set("EMSX_AMOUNT", 1000);
            request.Set("EMSX_ORDER_TYPE", "MKT");
            request.Set("EMSX_TIF", "DAY");
            request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            request.Set("EMSX_SIDE", "BUY");
            //request.Set("EMSX_TICKER", "IBM US Equity");
            //request.Set("EMSX_AMOUNT", 1000);
            //request.Set("EMSX_ORDER_TYPE", "MKT");
            //request.Set("EMSX_TIF", "DAY");
            //request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            //request.Set("EMSX_SIDE", "BUY");
            //request.Set("EMSX_BROKER", "BB");

            CorrelationID requestID = new CorrelationID(2);

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
            catch (Exception ex)
            {
                System.Console.Error.WriteLine("Failed to send the request: " + ex.Message);
            }
        
        
        }


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

             CorrelationID requestID = new CorrelationID(1);
             
             Service refDataSvc = session.GetService("//blp/refdata");
             Request request = refDataSvc.CreateRequest("ReferenceDataRequest");
             request.Append("securities", "IBM US Equity");
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


             RouteOrder(session);
             Console.ReadKey();
        }
    }
}

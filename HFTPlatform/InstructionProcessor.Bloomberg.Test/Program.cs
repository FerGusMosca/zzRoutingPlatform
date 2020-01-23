using Bloomberglp.Blpapi;
using Bussiness.Auxiliares;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsShared.Logging;

namespace HelloWorld.Bloomberg.Test
{
    class Program
    {
        #region Private Static Attributes

        private static int Sequence=1;

        protected static ILogSource AppLogger { get; set; }

        #endregion

        private static Session CreateSession()
        {
            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerHost = "localhost"; // default value
            sessionOptions.ServerPort = 8194; // default value

            Session session = new Session(sessionOptions);

            if (!session.Start())
            {
                Console.WriteLine("Could not start session.");

            }

            return session;
        }

        private static CorrelationID CreateCorrelation()
        {
            CorrelationID requestID = new CorrelationID(Sequence);
            Sequence++;
            return requestID;
        }

        private static Service OpenService(Session session, string service)
        { 
            
            if (!session.OpenService(service))
            {
                Console.WriteLine("Could not open service " + service);

            }

            Service svc = session.GetService(service);

            return svc;
        
        }

        private static void DoLog(string msg)
        {
            AppLogger.Debug(msg);
            Console.WriteLine(msg);
        }

        private static void handleResponseEvent(Event nevent)
        {
             Console.WriteLine("EventType =" + nevent.Type);
             IEnumerable<Message> messages  = nevent.GetMessages();


            foreach(Message message in  messages)
            {
                string msg = message.AsElement.ToString();

                DoLog(string.Format("CorrelationID {0}", message.CorrelationID));
                DoLog(string.Format("Type {0}", message.MessageType));

                DoLog(message.AsElement.ToString());

                //Element dvdHisElem=message.GetElement("securityData").GetValueAsElement().GetElement("fieldData").GetElement("DVD_HIST_ALL");
                //for (int i = 0; i < dvdHisElem.NumValues; i++)
                //{
                //    Element elems = (Element) dvdHisElem.GetValue(i);

                //    Datetime date = elems.GetElementAsDate("Ex-Date");
                   
                //}

                //foreach(var elem in elems)
                //{
                
                //}
            
            }
        }

        private static void LoopAndLogResponses(Session session)
        {
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

        private static void ShowActiveOrder(Event nevent, Session session)
        {
            IEnumerable<Message> messages = nevent.GetMessages();
            foreach (Message message in messages)
            {
                
                string MSG_SUB_TYPE = message.AsElement.GetElementAsString("MSG_SUB_TYPE");
                string EVENT_STATUS = message.AsElement.GetElementAsString("EVENT_STATUS");
                string EMSX_STATUS = message.AsElement.GetElementAsString("EMSX_STATUS");
                string EMSX_SEQUENCE = message.AsElement.GetElementAsString("EMSX_SEQUENCE");

                string EMSX_TICKER = message.AsElement.GetElementAsString("EMSX_TICKER");

                if (EVENT_STATUS == "7")
                {
                }
                else if (EVENT_STATUS == "8")
                {
                }
                else if (EVENT_STATUS == "9")
                {
                }
                else if (EVENT_STATUS == "4")
                {
                }


                Console.WriteLine(string.Format("Order {0} Symbol {1} Status {2}", EMSX_SEQUENCE, EMSX_TICKER, EMSX_STATUS));
              
            }
        
        }

        private static void DeleteActiveOrder(Event nevent, Session session)
        {
            IEnumerable<Message> messages = nevent.GetMessages();
            foreach (Message message in messages)
            {

                string MSG_SUB_TYPE = message.AsElement.GetElementAsString("MSG_SUB_TYPE");
                string EVENT_STATUS = message.AsElement.GetElementAsString("EVENT_STATUS");
                string EMSX_STATUS = message.AsElement.GetElementAsString("EMSX_STATUS");
                string EMSX_SEQUENCE = message.AsElement.GetElementAsString("EMSX_SEQUENCE");
                
                string EMSX_TICKER = message.AsElement.GetElementAsString("EMSX_TICKER");

                if (EVENT_STATUS == "7")
                {
                }
                else if (EVENT_STATUS == "8")
                {
                }
                else if (EVENT_STATUS == "9")
                {
                }
                else if (EVENT_STATUS == "4")
                {
                }

               

                if (EMSX_STATUS == "WORKING" && EMSX_SEQUENCE != null)
                {
                    string EMSX_ROUTE_ID = message.AsElement.GetElementAsString("EMSX_ROUTE_ID");
                    CancelOrder(session, EMSX_SEQUENCE,"1", Sequence);
                    Sequence++;
                    DeleteOrder(session, EMSX_SEQUENCE, Sequence);
                    Sequence++;
                }

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

     

        #region Routing Methods

        protected static void RouteOrder(Session session)
        {
            //AuthUser(session);

            //string svc = "//blp/emapisvc_beta";
            string svc = "//blp/emapisvc";

            session.OpenService(svc);

            Service service = session.GetService(svc);
            //Request request = service.CreateRequest("CreateOrder");
            Request request = service.CreateRequest("CreateOrderAndRouteEx");

             //The fields below are mandatory
            request.Set("EMSX_TICKER", "GS US Equity");
            request.Set("EMSX_AMOUNT", 120);
            request.Set("EMSX_ORDER_TYPE", "LMT");
            request.Set("EMSX_LIMIT_PRICE", 50);
            request.Set("EMSX_TIF", "DAY");
            request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            request.Set("EMSX_SIDE", "BUY");
            //request.Set("EMSX_BROKER", "EFIX");
            request.Set("EMSX_BROKER", "BMTB");
            request.Set("EMSX_ACCOUNT", "test124");
            //request.Set("EMSX_TICKER", "IBM US Equity");
            //request.Set("EMSX_AMOUNT", 1000);
            //request.Set("EMSX_ORDER_TYPE", "MKT");
            //request.Set("EMSX_TIF", "DAY");
            //request.Set("EMSX_HAND_INSTRUCTION", "ANY");
            //request.Set("EMSX_SIDE", "BUY");
            //request.Set("EMSX_BROKER", "BB");

            //Element strategy = request.GetElement("EMSX_STRATEGY_PARAMS");
            //strategy.SetElement("EMSX_STRATEGY_NAME", "VWAP");

            CorrelationID requestID = new CorrelationID(Sequence);
            Sequence++;

            string orderSequence = "";

            // Submit the request
            try
            {
               
               CorrelationID resp =  session.SendRequest(request, requestID);
               LoopAndLogResponses(session);
              SubscribeOrder(session, orderSequence);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine("Failed to send the request: " + ex.Message);
            }
        
        
        }

        protected static void CancelOrder(Session session, string EMSX_SEQUENCE, string EMSX_ROUTE_ID, int correlationID)
        {
            Service service = session.GetService("//blp/emapisvc_beta");

            Request request = service.CreateRequest("CancelRoute");

            //request.set("EMSX_REQUEST_SEQ", 1);
            //request.set("EMSX_TRADER_UUID", 1234567);

            Element routes = request.GetElement("ROUTES"); //Note, the case is important.
            Element route = routes.AppendElement(); // Multiple routes can be cancelled in a single request
            route.GetElement("EMSX_SEQUENCE").SetValue(EMSX_SEQUENCE);
            route.GetElement("EMSX_ROUTE_ID").SetValue(EMSX_ROUTE_ID);

            //request.GetElement("EMSX_SEQUENCE").AppendValue(EMSX_SEQUENCE);

            CorrelationID requestID = new CorrelationID(correlationID);
            session.SendRequest(request, requestID);

            
            
        }

        protected static void DeleteOrder(Session session, string EMSX_SEQUENCE, int correlationID)
        {
            Service service = session.GetService("//blp/emapisvc_beta");

            Request request = service.CreateRequest("DeleteOrder");

            request.GetElement("EMSX_SEQUENCE").AppendValue(EMSX_SEQUENCE);

            CorrelationID requestID = new CorrelationID(correlationID);
            session.SendRequest(request, requestID);
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

        protected static void GetAllActiveOrders(Session session)
        {
            //session.OpenService("//blp/emapisvc_beta");
            CorrelationID requestID = new CorrelationID(98);

            session.OpenService("//blp/emapisvc");

            //Service service = session.GetService("//blp/emapisvc_beta");

            List<Subscription> subscriptions = new List<Subscription>();
            //string strSubscriptionFields = "//blp/emapisvc_beta/order?fields=";
            string strSubscriptionFields = "//blp/emapisvc/order?fields=";
            //string strSubscriptionFields = "//blp/emapisvc/route?fields=";
            strSubscriptionFields += "EMSX_TICKER,";
            strSubscriptionFields += "EMSX_STATUS,";
            strSubscriptionFields += "EMSX_ROUTE_ID,";
            strSubscriptionFields += "EMSX_SEQUENCE";
        
            Subscription subscription = new Subscription(strSubscriptionFields,requestID);
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
                            //DeleteActiveOrder(nevent,session);
                            ShowActiveOrder(nevent, session);
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

        private static void RequestCurveList(Session session)
        {
            Service curveService = OpenService(session,"//blp/instruments");
            Request request = curveService.CreateRequest("curveListRequest");
            request.AsElement.SetElement("query", "GOLD");
            request.AsElement.SetElement("bbgid", "YCCD1016");
            request.AsElement.SetElement("countryCode", "US");
            request.AsElement.SetElement("currencyCode", "USD");
            request.AsElement.SetElement("curveid", "CD1016");
            request.AsElement.SetElement("type", "CORP");
            request.AsElement.SetElement("subtype", "CDS");
            request.AsElement.SetElement("maxResults", "10");
            session.SendRequest(request, CreateCorrelation());

            LoopAndLogResponses(session);
      
        }

        private static void GetFills(Session session)
        {

            Service curveService = OpenService(session, "//blp/emsx.history");
            Request request = curveService.CreateRequest("GetFills");

            request.Set("FromDateTime", "2019-09-06T00:00:00.000+00:00");
            request.Set("ToDateTime", "2019-11-06T23:59:00.000+00:00");

            session.SendRequest(request, CreateCorrelation());

            LoopAndLogResponses(session);
        
        }

        private static void GetDivHist(Session session)
        {
            Service refDataSvc = OpenService(session, "//blp/refdata");

            Request request = refDataSvc.CreateRequest("ReferenceDataRequest");
            request.Append("securities", "NKE US Equity");
            request.Append("fields", "DVD_HIST_ALL");
            //request.Append("fields", "EQY_DVD_HIST_SPLITS");

            session.SendRequest(request, CreateCorrelation());
            LoopAndLogResponses(session);
        }

        private static void PortfolioDataRequest(Session session)
        {
            Service refDataSvc = OpenService(session, "//blp/refdata");
          
            Request request = refDataSvc.CreateRequest("PortfolioDataRequest");
            request.AsElement.SetElement("securities", "F");

            session.SendRequest(request, CreateCorrelation());
            LoopAndLogResponses(session);
        }

        private static void RequestFields(Session session)
        {
            Service refDataSvc = OpenService(session, "//blp/apiflds");
            Request request = refDataSvc.CreateRequest("CategorizedFieldSearchRequest");
            request.Set("searchSpec", "split");

            session.SendRequest(request, CreateCorrelation());
            LoopAndLogResponses(session);
        }

        private static void GetBrokerSpec(Session session)
        {
            CorrelationID requestID = new CorrelationID(96);
            Service service = OpenService(session, "//blp/emsx.brokerspec");
            Request request = service.CreateRequest("GetBrokerSpecForUuid");

            request.Set("uuid", 22748905);//

            session.SendRequest(request, requestID);
            LoopAndLogResponses(session);        
        }

        private static void GetStrategyInfo(Session session)
        {
            CorrelationID requestID = new CorrelationID(98);
            Service service = OpenService(session, "//blp/emapisvc");
            Request request = service.CreateRequest("GetBrokerStrategyInfo");

            TimeSpan elapsed = DateTime.Now - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            request.Set("EMSX_REQUEST_SEQ", Convert.ToInt32(elapsed.TotalSeconds));
                
            request.Set("EMSX_ASSET_CLASS","EQTY") ;//
            request.Set("EMSX_BROKER",ConfigurationManager.AppSettings["Broker"]);
            request.Set("EMSX_STRATEGY", ConfigurationManager.AppSettings["Strategy"]);

            session.SendRequest(request, requestID);
            LoopAndLogResponses(session);
        
        }

        private static void RequestMktDataTest(Session session)
        {
            Service refDataSvc = OpenService(session, "//blp/refdata");
       
            Request request = refDataSvc.CreateRequest("ReferenceDataRequest");

            request.Append("securities", "DICAC@BUE AR Govt");
            //request.Append("securities", "GGAL@BUE AR Equity");
            request.Append("fields", "PR005");
            request.Append("fields", "OPEN");
            request.Append("fields", "PX_LAST");
            request.Append("fields", "10_YEAR_MOVING_AVERAGE_PE");

            session.SendRequest(request, CreateCorrelation());
            LoopAndLogResponses(session);
        }

        #endregion

        static void Main(string[] args)
        {
            string configFile = Const.ConfigFileDefault;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            configFile = Directory.GetCurrentDirectory() + "\\" + configFile;

            AppLogger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\Log", Directory.GetCurrentDirectory() + "\\Log\\Backup")
            {
                FilePattern = "Log.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };

            Session session = CreateSession();

            //Service service = session.GetService("//blp/emapisvc_beta");
           // RequestMktDataTest(session);
            //GetStrategyInfo(session);
            GetBrokerSpec(session);
            //SubscribeOrder(session,"");
            //RequestFields(session);
            //GetDivHist(session);
            //RequestCurveList(session);
            //RouteOrder(session);
            //PortfolioDataRequest(session);
            //GetFills(session);
            //GetAllActiveOrders(session);



             Console.ReadKey();
        }
    }
}

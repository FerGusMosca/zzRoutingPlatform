using Bussiness.Auxiliares;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsShared.Logging;
using zHFT.Main;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;

namespace PrimaryCertification
{
    class Program
    {
        #region static Private Attributes

        protected static MainApp App { get; set; }

        protected static string Account { get; set; }

        private static bool ToConsole { get; set; }

        #endregion

        #region Protected Static Methods

        protected static void DoLog(string msg, Constants.MessageType type)
        {
            if (ToConsole)
                Console.WriteLine(msg);
            else if (msg.StartsWith("toConsole->"))
            {
                Console.WriteLine(msg.Replace("toConsole->", ""));
                Console.WriteLine("");
            }
        }

        protected static Side GetOrderSide(string side)
        {
            if (side == "B")
                return Side.Buy;
            else if (side == "S")
                return Side.Sell;
            else
                throw new Exception(string.Format("Side no reconocido: {0}", side));
        
        }

        protected static OrdType GetOrderType(string ordType)
        {
            if (ordType == "LMT")
                return OrdType.Limit;
            else if (ordType == "MKT")
                return OrdType.Market;
            else
                throw new Exception(string.Format("OrdType no reconocido: {0}", ordType));

        }

        protected static void ProcessNewOrderSingleCommand(string command)
        {
            string[] fields = command.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length < 6)
                throw new Exception("Comando NOS con mal formato");

            try
            {
                string symbol = fields[1].Trim();
                string exchange = fields[2].Trim();
                string side = fields[3].Trim();
                string ordType = fields[4].Trim();

                double ordQty = double.Parse(fields[5].Trim());
                double? price = null;

                if (fields.Length > 6)
                {
                    price = double.Parse(fields[6].Trim());
                }


                Order order = new Order();
                order.ClOrdId = Guid.NewGuid().ToString();
                order.Security = new Security() { Symbol = symbol, SecType = SecurityType.CS };
                order.Symbol = symbol;
                order.Exchange = exchange;
                order.Side = GetOrderSide(side);
                order.OrdType = GetOrderType(ordType);
                order.OrderQty = ordQty;
                order.Price = price;
                order.Account = Account;
                

                NewOrderWrapper noWrapper = new NewOrderWrapper(order, null);

                App.ProcessMessageToOutgoing(noWrapper);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error intentando procesar los campos de la nueva orden: {0}", ex.Message));
            }
        }

        protected static void ProcessMarketDataRequest(string command)
        {
            string[] fields = command.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != 3)
                throw new Exception("Comando MD con mal formato");

            string symbol = fields[1].Trim();
            string exchange = fields[2].Trim();

            Security sec = new Security() { Symbol = symbol, Exchange = exchange, SecType = SecurityType.CS };

            MarketDataRequestWrapper mdrWrapper = new MarketDataRequestWrapper(sec, SubscriptionRequestType.SnapshotAndUpdates);
            App.ProcessMessageToIncoming(mdrWrapper);
        }

        protected static void ProcessCancelOrderCommand(string command)
        {
            string[] fields = command.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

             if (fields.Length < 2)
                throw new Exception("Comando CO con mal formato");

            Order order = new Order();
            order.OrigClOrdId = fields[1].Trim();
            order.ClOrdId = fields[1].Trim();

            zHFT.OrderRouters.Common.Wrappers.CancelOrderWrapper cancelWrapper = new zHFT.OrderRouters.Common.Wrappers.CancelOrderWrapper(order, null);

            App.ProcessMessageToOutgoing(cancelWrapper);
        }

        protected static void ProcessUpdateOrderCommand(string command)
        {
            string[] fields = command.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != 4)
                throw new Exception("Comando UO con mal formato");

            Order order = new Order();
            order.ClOrdId = fields[1].Trim();
            order.OrderQty = Convert.ToDouble(fields[2].Trim());
            order.Price = Convert.ToDouble(fields[3].Trim());

            zHFT.OrderRouters.Common.Wrappers.UpdateOrderWrapper updateWrapper = new zHFT.OrderRouters.Common.Wrappers.UpdateOrderWrapper(order, null);

            App.ProcessMessageToOutgoing(updateWrapper);
        }

        protected static void ProcessMassStatusRequestWrapper()
        {
            Console.Clear();
            
            OrderMassStatusRequestWrapper omsReq = new OrderMassStatusRequestWrapper();
            
            App.ProcessMessageToOutgoing(omsReq);
            
        }

        protected static void ProcessListOrdersRequest(string command)
        {
            Console.Clear();

            OrderListWrapper wrapper = new OrderListWrapper();

            App.ProcessMessageToOutgoing(wrapper);
        }

        protected static void ProcessCommand(string command)
        {
            if (command == "cls")
            {
                Console.Clear();
            }
            else if (command == "SL")
            {
                SecurityListRequestWrapper slWrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities,null);
                App.ProcessMessageToIncoming(slWrapper);
            }
            else if (command.StartsWith("NOS"))
            {
                ProcessNewOrderSingleCommand(command);
            
            }
            else if (command.StartsWith("CO"))
            {
                ProcessCancelOrderCommand(command);

            }
            else if (command.StartsWith("UO"))
            {
                ProcessUpdateOrderCommand(command);

            }
            else if (command.StartsWith("MD"))
            {
                ProcessMarketDataRequest(command);
            }
            else if (command.StartsWith("LO"))
            {
                ProcessListOrdersRequest(command);
            }
            else if (command.StartsWith("MO"))
            {
                ProcessMassStatusRequestWrapper();
            }
        }

        protected static void Run()
        {
            string archivoConfig = Const.ConfigFileDefault;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            archivoConfig = Directory.GetCurrentDirectory() + "\\" + archivoConfig;
            ToConsole = Convert.ToBoolean(ConfigurationManager.AppSettings["allwaysToConsole"]);
            Account = ConfigurationManager.AppSettings["Account"];
            ILogSource appLogger;
            ILogSource incommingLogger;
            ILogSource outgoingLogger;

            appLogger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\Log", Directory.GetCurrentDirectory() + "\\Log\\Backup")
            {
                FilePattern = "Log.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };

            incommingLogger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\incoming", Directory.GetCurrentDirectory() + "\\Backup")
            {
                FilePattern = "Incoming.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };

            outgoingLogger = new PerDayFileLogSource(Directory.GetCurrentDirectory() + "\\outgoing", Directory.GetCurrentDirectory() + "\\Backup")
            {
                FilePattern = "Outgoing.{0:yyyy-MM-dd}.log",
                DeleteDays = 20
            };

            App = new MainApp(incommingLogger, outgoingLogger, appLogger, archivoConfig, Program.DoLog);

            App.Run();
        
        }

        #endregion

        static void Main(string[] args)
        {
            Run();
            string command = "";

            while (command != "q")
            {

                Console.WriteLine("--------------------");
                Console.WriteLine("Ingrese comando");
                Console.WriteLine("SL-Security List Request");
                Console.WriteLine("MD-Market Data Request. Ejemplo: MD GGAL BUE");
                Console.WriteLine("NOS-New Order Single. Ejemplo: NOS GGAL BUE <Side:B/S> <OrdType:LMT/MKT> <qty> <price>");//B=Buy,LMT=Limit,100=Qty,80=Limit Price
                Console.WriteLine("CO-Cancel Order. Ejemplo: CO <ClOrderId>");//100 es el Id de la orden
                Console.WriteLine("UO-Cancel Order. Ejemplo: CO <ClOrderId> <qty> <price>");//100 es el Id de la orden
                Console.WriteLine("LO-List Orders.");
                Console.WriteLine("MO-Order Mass Status Request.");
                Console.WriteLine("cls-Limpiar Pantalla");
                Console.WriteLine("q-Quit");


                command = Console.ReadLine();
                try
                {
                    ProcessCommand(command);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error processing command {0}:{1}", command, ex.Message));
                
                }
            }
            
        }
    }
}

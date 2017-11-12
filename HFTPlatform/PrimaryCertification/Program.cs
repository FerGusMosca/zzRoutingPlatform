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
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.SecurityListSaver.Common.Wrappers;

namespace PrimaryCertification
{
    class Program
    {
        #region static Private Attributes

        protected static MainApp App { get; set; }

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
            else if (command.StartsWith("MD"))
            {
                string[] fields = command.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (fields.Length != 3)
                    throw new Exception("Comando MD con mal formato");

                string symbol = fields[1].Trim();
                string exchange = fields[2].Trim();

                Security sec = new Security() { Symbol = symbol, Exchange = exchange, SecType = SecurityType.CS };

                MarketDataRequestWrapper mdrWrapper = new MarketDataRequestWrapper(sec);
                App.ProcessMessageToIncoming(mdrWrapper);
            
            }
        }

        protected static void Run()
        {
            string archivoConfig = Const.ConfigFileDefault;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            archivoConfig = Directory.GetCurrentDirectory() + "\\" + archivoConfig;
            ToConsole = Convert.ToBoolean(ConfigurationManager.AppSettings["allwaysToConsole"]);
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

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
using zHFT.Main.Common.Util;

namespace OrderImbalanceCalculator.Test
{
    class Program
    {
        private static bool ToConsole { get; set; }

        private static bool OnlyLogImbalanceInfo { get; set; }

        private static string ImbalanceInfoPrefix { get; set; }

        public static void DoLog(string msg, Constants.MessageType type)
        {
            ConsoleDisplayer.GetInstance(ToConsole).DoLog(msg, type);


        }

        static void Main(string[] args)
        {
            string archivoConfig = Const.ConfigFileDefault;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            archivoConfig = Directory.GetCurrentDirectory() + "\\" + archivoConfig;
            ToConsole = Convert.ToBoolean(ConfigurationManager.AppSettings["allwaysToConsole"]);
            OnlyLogImbalanceInfo = Convert.ToBoolean(ConfigurationManager.AppSettings["OnlyLogImbalanceInfo"]);
            ImbalanceInfoPrefix = ConfigurationManager.AppSettings["ImbalanceInfoPrefix"];
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

            MainApp app = new MainApp(incommingLogger, outgoingLogger, appLogger, archivoConfig, Program.DoLog);

            app.Run();

            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
        }
    }
}

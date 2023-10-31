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

namespace MarketDataDownloader
{
    class Program
    {
        private static bool ToConsole { get; set; }

        private static MainApp App { get; set; }


        public static void DoLog(string msg, Constants.MessageType type)
        {
            if (!App.EvalLogging(type))
                return;

            if (type == zHFT.Main.Common.Util.Constants.MessageType.Error ||
                type == zHFT.Main.Common.Util.Constants.MessageType.Exception)
                Console.ForegroundColor = ConsoleColor.Red;

            if (type == zHFT.Main.Common.Util.Constants.MessageType.Debug)
                Console.ForegroundColor = ConsoleColor.Yellow;

            if (ToConsole)
                Console.WriteLine(msg);
            else if (msg.StartsWith("toConsole->"))
            {
                Console.WriteLine(msg.Replace("toConsole->", ""));
                Console.WriteLine("");
            }

            Console.ResetColor();
        }

        static void Main(string[] args)
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

            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
        }
    }
}

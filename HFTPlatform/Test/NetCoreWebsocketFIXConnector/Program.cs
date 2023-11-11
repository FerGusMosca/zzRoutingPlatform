using System;
using System.Configuration;
using System.IO;
using Bussiness.Auxiliares;
using ToolsShared.Logging;
using zHFT.Main;
using zHFT.Main.Common.Util;

namespace NetCoreWebsocketFIXConnector
{
    internal class Program
    {
        #region Private Static Consts
        
        private static bool ToConsole { get; set; }

        private static string DebugLevel { get; set; }

        #endregion

        #region Public Static Methods

        public static void DoLog(string msg, Constants.MessageType type)
        {
            if (DebugLevel == "INFO" && type == Constants.MessageType.Debug)
                return;


            ConsoleDisplayer.GetInstance(ToConsole).DoLog(msg, type);
        }

        #endregion
        
        public static void Main(string[] args)
        {
            string archivoConfig = Const.ConfigFileDefault;
            DebugLevel = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["DebugLevel"]) ? ConfigurationManager.AppSettings["DebugLevel"] : "DEBUG";
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

            MainApp app = new MainApp(incommingLogger, outgoingLogger, appLogger, archivoConfig, Program.DoLog);

            app.Run();

            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
        }
    }
}
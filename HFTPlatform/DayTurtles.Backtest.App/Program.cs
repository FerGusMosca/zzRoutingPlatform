using Bussiness.Auxiliares;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToolsShared.Logging;
using zHFT.Main;
using zHFT.Main.Common.Util;

namespace DayTurtles.Backtest.App
{
    public class Program
    {
        private static bool ToConsole { get; set; }

        public static void DoLog(string msg, Constants.MessageType type)
        {
            ConsoleDisplayer.GetInstance(ToConsole).DoLog(msg, type);
        }

        public static void Main(string[] args)
        {

            string archivoConfig = Const.ConfigFileDefault;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            archivoConfig = Directory.GetCurrentDirectory() + "\\" + archivoConfig;
            ToConsole = Convert.ToBoolean(ConfigurationManager.AppSettings["allwaysToConsole"]);
            DateTime from = Convert.ToDateTime(ConfigurationManager.AppSettings["from"]);
            DateTime to = Convert.ToDateTime(ConfigurationManager.AppSettings["to"]);
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

            DateTime today = from;

            while (DateTime.Compare(today, to) <= 0)
            {

                DateTimeManager.Now = today;
                TradingBacktestingManager.StartTradingDay();

                DoLog($"========Starting Trading Day for {today}========", Constants.MessageType.Information);
                MainApp app = new MainApp(incommingLogger, outgoingLogger, appLogger, archivoConfig, Program.DoLog);

                app.Run();

                while (TradingBacktestingManager.IsTradingDayActive())
                {
                    Thread.Sleep(100);
                }
                Thread.Sleep(10000);//wait prevmarket to close everything
                today = today.AddDays(1);
            }

            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
        }
    }
}

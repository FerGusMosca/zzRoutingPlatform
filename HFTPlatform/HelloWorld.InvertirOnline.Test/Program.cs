using Bussiness.Auxiliares;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsShared.Logging;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.IBR.IOL.DataAccessLayer;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;

namespace HelloWorld.InvertirOnline.Test
{
    class Program
    {
        private static bool ToConsole { get; set; }

        public static event OnLogMessage onLog;

        public static void DoLog(string msg, Constants.MessageType type)
        {
            if (ToConsole)
                Console.WriteLine(msg);
            else if (msg.StartsWith("toConsole->"))
            {
                Console.WriteLine(msg.Replace("toConsole->", ""));
                Console.WriteLine("");
            }
        }

        static void Main(string[] args)
        {
            onLog += DoLog;

            //MainApp app = new MainApp(incommingLogger, outgoingLogger, appLogger, archivoConfig, Program.DoLog);

            //app.Run();

            List<ConfigKey> configs = new List<ConfigKey>();

            configs.Add(new ConfigKey() { Key = "AccountNumber", Value = ConfigurationManager.AppSettings["AccountNumber"] });
            configs.Add(new ConfigKey() { Key = "ConfigConnectionString", Value = ConfigurationManager.AppSettings["ConfigConnectionString"] });

            IOLAccountManager IOLAccountManager = new IOLAccountManager(onLog, configs);


            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
        }
    }
}

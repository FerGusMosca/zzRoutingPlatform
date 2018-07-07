using Bussiness.Auxiliares;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
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

            //You will have to make something in C++ so that you can skip ahead the certificate validation
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


            List<ConfigKey> configs = new List<ConfigKey>();

            configs.Add(new ConfigKey() { Key = "AccountNumber", Value = ConfigurationManager.AppSettings["AccountNumber"] });
            configs.Add(new ConfigKey() { Key = "ConfigConnectionString", Value = ConfigurationManager.AppSettings["ConfigConnectionString"] });
            configs.Add(new ConfigKey() { Key = "LoginTokenURL", Value = ConfigurationManager.AppSettings["LoginTokenURL"] });

            IOLAccountManager IOLAccountManager = new IOLAccountManager(onLog, configs);

            Console.WriteLine("Press Enter to quit.");
            Console.ReadLine();
        }
    }
}

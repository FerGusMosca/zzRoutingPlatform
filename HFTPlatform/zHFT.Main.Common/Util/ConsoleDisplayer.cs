using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;

namespace zHFT.Main.Common.Util
{
    public class ConsoleDisplayer : ILogger
    {
        #region Protected Attributes

        protected static bool ToConsole = true;

        protected static ConsoleDisplayer Instance { get; set; }

        #endregion

        #region Constructor

        private ConsoleDisplayer()
        { 
        }

        private ConsoleDisplayer(bool pToConsole)
        {
            ToConsole = pToConsole;
        }

        #endregion

        #region Public Static Methods

        public static ConsoleDisplayer GetInstance()
        {
            if (Instance == null)
                Instance = new ConsoleDisplayer();

            return Instance;

        }

        public static ConsoleDisplayer GetInstance(bool pToConsole)
        {
            if (Instance == null)
                Instance = new ConsoleDisplayer(pToConsole);

            return Instance;

        }

        #endregion

        #region Public Methods


        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            throw new NotImplementedException();
        }

        public void DoLogLight(string msg, Constants.MessageType type)
        {
            if (type == Constants.MessageType.PriorityInformation 
                || type == Constants.MessageType.Error 
                )
            {
                DoLog(msg, type);
            }

        }

        public  void DoLog(string msg, Constants.MessageType type)
        {
            if (type == Constants.MessageType.Debug)
                return;

            try
            {

                Console.BackgroundColor = ConsoleColor.Red;


                if (ToConsole)
                    Console.WriteLine(msg);
                else if (msg.StartsWith("toConsole->"))
                {
                    Console.WriteLine(msg.Replace("toConsole->", ""));
                    Console.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to std out:{ex.Message}");

            }
            finally
            {

                Console.ResetColor();
            }
        }

        #endregion
    }
}

using System;
using zHFT.Main.Common.Util;

namespace zHFT.Main.Common.Util
{
    public class Logger
    {
        #region Protected Static Consts

        protected static string _INFO = "INFO";
        protected static string _DEBUG = "DEBUG";

        #endregion
        
        #region Protected Attributes
        
        protected string DebugLevel { get; set; }
        
        #endregion
        
        #region Constructors

        public Logger(string pDebugLevel)
        {
            DebugLevel = pDebugLevel;

        }

        #endregion

        #region Protected Static Methods

        //This should be logged, but we will write it on the screen for simplicity
        public  void DoLog(string message,Constants.MessageType type)
        {
            if (type == Constants.MessageType.Debug && DebugLevel != _DEBUG)
                return;

            if (type == Constants.MessageType.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                //Logger.Debug(msg, type);
            }
            else if (type == Constants.MessageType.Information)
            {
                //Logger.Debug(msg, type);
            }
            else if (type == Constants.MessageType.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                //Logger.Alert(msg, type);
            }
            else if (type == Constants.MessageType.EndLog)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                //Logger.Debug(msg, type);
            }
            else if (type == Constants.MessageType.Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                //Logger.Debug(msg, type);
            }

            Console.WriteLine(message);

            Console.ResetColor();
        }

        #endregion
    }
}
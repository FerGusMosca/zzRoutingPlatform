using Bussiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsShared.Logging;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;

namespace zHFT.Main.Common.Apps
{
    public abstract class MainAppBase : IApplication
    {
        #region Protected Attributes

        public Bussiness.ApplicationBase.ApplicationState State { get; set; }

        protected string ConfigFile { get; set; }

        protected ILogSource MessageIncomingLogger { get; set; }

        protected ILogSource MessageOutgoingLogger { get; set; }

        protected ILogSource appLogger;

        #endregion

        #region Abstract Methods

        protected abstract CMState ProcessOutgoing(Wrapper wrapper);

        protected abstract CMState ProcessIncoming(Wrapper wrapper);

        public abstract void Run();

        public abstract void CloseSession();

        #endregion

        #region Protected Methods

        public void Log(string message)
        {
            if (appLogger != null)
                appLogger.Debug(message);
        }

        public void Log(string message, Constants.MessageType tipo)
        {
            if (appLogger != null)
                appLogger.Debug(message, tipo);
        }

        public void Log(string message, params object[] parameters)
        {
            if (appLogger != null)
                appLogger.Debug(message, parameters);
        }


        #endregion
     
    }
}

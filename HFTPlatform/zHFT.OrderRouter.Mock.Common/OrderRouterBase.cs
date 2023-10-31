using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouter.Mock.Common
{
    public abstract class OrderRouterBase : ICommunicationModule
    {
        #region Public Attributes
        public string ModuleConfigFile { get; set; }
        protected OnLogMessage OnLogMsg { get; set; }
        protected OnMessageReceived OnMessageRcv { get; set; }

        #endregion

        #region Abstract Methods

        public abstract CMState ProcessMessage(Wrapper wrapper);

        public abstract bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile);

        #endregion

        #region Protected Methods

        public void DoLog(string msg, Main.Common.Util.Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        #endregion
    }
}

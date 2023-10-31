using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.InvertirOnline.Common
{
    public abstract class OrderRouterBase : ICommunicationModule
    {
        #region Protected Attributes

        protected string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected IConfiguration Config { get; set; }

        protected int NextOrderId { get; set; }

        #endregion

        #region Abstract Methods

        public abstract CMState ProcessMessage(Wrapper wrapper);

        public abstract bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile);

        protected abstract void DoLoadConfig(string configFile, List<string> noValueFields);

        protected abstract CMState ProcessIncoming(Wrapper wrapper);

        protected abstract CMState ProcessOutgoing(Wrapper wrapper);

        //protected abstract void ProcessOrderStatus(OrderStatusDTO dto);

        //protected abstract void ProcessOrderError(int id, int errorCode, string errorMsg);

        #endregion

        #region Protected Methods

        protected void DoLog(string msg, Main.Common.Util.Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        protected bool LoadConfig(string configFile)
        {
            DoLog(DateTime.Now.ToString() + "OrderRouterBase.LoadConfig", Main.Common.Util.Constants.MessageType.Information);

            DoLog("Loading config:" + configFile, Main.Common.Util.Constants.MessageType.Information);
            if (!File.Exists(configFile))
            {
                DoLog(configFile + " does not exists", Main.Common.Util.Constants.MessageType.Error);
                return false;
            }

            List<string> noValueFields = new List<string>();
            DoLog("Processing config:" + configFile, Main.Common.Util.Constants.MessageType.Information);
            try
            {
                DoLoadConfig(configFile, noValueFields);
                DoLog("Ending GetConfiguracion " + configFile, Main.Common.Util.Constants.MessageType.Information);
            }
            catch (Exception e)
            {
                DoLog("Error recovering config " + configFile + ": " + e.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }

            if (noValueFields.Count > 0)
                noValueFields.ForEach(s => DoLog(string.Format(Main.Common.Util.Constants.FieldMissing, s), Main.Common.Util.Constants.MessageType.Error));

            return true;
        }

        #endregion
    }
}

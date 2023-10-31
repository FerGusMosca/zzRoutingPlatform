using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.MarketClient.Common;

namespace zHFT.MarketClient.Quickfix.Common
{
    public abstract class QuickfixMarketClientBase : MarketClientBase, Application
    {
        #region Private And Protected Attributes
        protected OnLogMessage OnLogMsg { get; set; }
        protected OnMessageReceived OnMessageRcv { get; set; }

        public string ModuleConfigFile { get; set; }
        protected SessionID SessionID { get; set; }

        protected SessionSettings SessionSettings { get; set; }
        protected FileStoreFactory FileStoreFactory { get; set; }
        protected ScreenLogFactory ScreenLogFactory { get; set; }
        protected MessageFactory MessageFactory { get; set; }
        #endregion

        #region Protected Methods

        protected void ProcessFixMessage(QuickFix.Message message, SessionID sessionId)
        {
            if (OnMessageRcv != null)
            {
                try
                {
                    //El New Orde Converter se encarga de todas las conversiones desde FIX
                   // OnMessageRcv(NewOrdenCoverter.ConvertFromFIX(message, GetConfig()));
                }
                catch (Exception ex)
                {

                    DoLog("There was an error processing the fix message:" + ex.Message, Constants.MessageType.Error);
                }
            }
            else
            {
                DoLog("The message could not be processed because there is not a callback defined", Constants.MessageType.Error);
            }
        }

        protected CMState DoSend(Message msg, Actions action)
        {
            if (SessionID == null)
                throw new Exception("Session not initialized with destiny for action " + action.ToString());

            if (Session.doesSessionExist(SessionID))
            {
                return CMState.BuildSuccess(Session.sendToTarget(msg, SessionID), null);
            }
            else
                throw new Exception("Invalid Session for action " + action.ToString() + ". Reconnect");
        }

        #endregion

        #region Application Members

        public abstract void fromApp(Message value, SessionID sessionId);

        public abstract void fromAdmin(Message value, SessionID sessionId);

        public abstract void onCreate(SessionID value);

        public abstract void onLogon(SessionID value);

        public abstract void onLogout(SessionID value);

        public abstract void toAdmin(Message value, SessionID sessionId);

        public abstract void toApp(Message value, SessionID sessionId);

        #endregion
    }
}

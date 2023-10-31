using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Security_List;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.Common.Converters;

namespace zHFT.InstructionBasedFullMarketConnectivity
{
    public abstract class BaseFullMarketConnectivity : Application
    {
        #region Private  Consts

        protected int _SECURITIES_REMOVEL_PERIOD = 60 * 60 * 1000;//Once every hour in milliseconds

        protected int _MAX_ELAPSED_HOURS_FOR_MARKET_DATA = 12;

        protected string _DUMMY_SECURITY = "kcdlsncslkd";

        #endregion

        #region Protected Attributes

        protected IFIXMessageCreator FIXMessageCreator { get; set; }
        protected SessionSettings SessionSettings { get; set; }
        protected FileStoreFactory FileStoreFactory { get; set; }
        protected ScreenLogFactory ScreenLogFactory { get; set; }
        protected SessionID SessionID { get; set; }
        protected MessageFactory MessageFactory { get; set; }
        protected SocketInitiator Initiator { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected object tLock = new object();

        protected object tLockSavingMarketData = new object();

        protected int MarketDataRequestId { get; set; }

        protected int OrderIndexId { get; set; }

        protected SecurityListConverter SecurityListConverter { get; set; }

        protected Dictionary<string, Order> ActiveOrders { get; set; }

        protected Dictionary<string, int> ActiveOrderIdMapper { get; set; }

        protected Dictionary<string, int> ReplacingActiveOrderIdMapper { get; set; }

        #endregion

        #region Abstract Methods

        public abstract BaseConfiguration GetConfig();

        protected abstract void ProcessSecurities(SecurityList securityList);

        protected abstract void CancelMarketData(Security sec);

        #endregion

        #region Abstract QuickFix Methods
        public abstract void fromApp(Message value, SessionID sessioId);
        public abstract void toAdmin(Message value, SessionID sessioId);
        #endregion

        #region Quickfix Methods

        public virtual void fromAdmin(Message value, SessionID sessionId)
        {
            DoLog("Invocación de fromAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
        }

        public virtual void onCreate(SessionID value)
        {
            DoLog("Invocación de onCreate : " + value.ToString(), Constants.MessageType.Information);
        }

        public virtual void onLogon(SessionID value)
        {

            SessionID = value;
            DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

            if (SessionID != null)
                DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);
            else
                DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);

        }

        public virtual void onLogout(SessionID value)
        {
            SessionID = null;
            DoLog("Invocación de onLogout : " + value.ToString(), Constants.MessageType.Information);
        }

        public virtual void toApp(Message value, SessionID sessionId)
        {
            DoLog("Invocación de toApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
        }

        #endregion

        #region Protected Methods

        protected int GetNextOrderId()
        {

            DateTime dayStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

            TimeSpan span = DateTime.Now - dayStart;

            return Convert.ToInt32(span.TotalSeconds);

        }

        #endregion

        #region Public Methods

        public void DoLog(string msg, Main.Common.Util.Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        #endregion
    }
}

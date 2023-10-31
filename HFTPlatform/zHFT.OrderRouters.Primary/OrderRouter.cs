using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Primary
{
    public class OrderRouter : BaseCommunicationModule, Application
    {
        #region Private Consts

        private string _DUMMY_SECURITY = "kcdlsncslkd";

        #endregion

        #region Private Attributes

        public IConfiguration Config { get; set; }

        protected Common.Configuration.Configuration PrimaryConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected SessionSettings SessionSettings { get; set; }
        protected FileStoreFactory FileStoreFactory { get; set; }
        protected ScreenLogFactory ScreenLogFactory { get; set; }
        protected SessionID SessionID { get; set; }
        protected MessageFactory MessageFactory { get; set; }
        protected SocketInitiator Initiator { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        #endregion

        #region Protected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion

        #region Public Methods

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {

                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    DoLog(string.Format("@{0}:Routing with Primary to market for symbol {1}",PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    //RouteNewOrder(wrapper);

                }
                else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
                {
                    DoLog(string.Format("@{0}:Updating order with Primary  for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    //UpdateOrder(wrapper, false);

                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    DoLog(string.Format("@{0}:Canceling order with Primary  for symbol {1}", PrimaryConfiguration.Name, wrapper.GetField(OrderFields.Symbol).ToString()), Main.Common.Util.Constants.MessageType.Information);
                    //UpdateOrder(wrapper, true);
                }
                else
                {
                    DoLog(string.Format("@{0}:Could not process order routing for action {1} with Primary:", PrimaryConfiguration.Name, wrapper.GetAction().ToString()),
                          Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildFail(new Exception(string.Format("@{0}:Could not process order routing for action {1} with Primary:", PrimaryConfiguration.Name, wrapper.GetAction().ToString())));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error routing order to market using Primary:{1}", PrimaryConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    SessionSettings = new SessionSettings(PrimaryConfiguration.FIXInitiatorPath);
                    FileStoreFactory = new FileStoreFactory(SessionSettings);
                    ScreenLogFactory = new ScreenLogFactory(SessionSettings);
                    MessageFactory = new DefaultMessageFactory();

                    Initiator = new SocketInitiator(this, FileStoreFactory, SessionSettings, ScreenLogFactory, MessageFactory);

                    Initiator.start();

                    return true;

                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile, PrimaryConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing {1}:{2}", PrimaryConfiguration.Name,
                                                                              configFile,
                                                                              ex.Message),
                                                                              Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion

        #region QuickFix Methods

        public void fromAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog(string.Format("@{0}-Invocación de fromAdmin por la sesión {1}:{2}", PrimaryConfiguration.Name, sessionId.ToString(), value.ToString()),
                      Constants.MessageType.Information);
            }
        }

        public void fromApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog(string.Format("@{0}-Invocación de fromApp por la sesión {1}:{2}", PrimaryConfiguration.Name, sessionId.ToString(), value.ToString()),
                    Constants.MessageType.Information);
                //TO DO: Completar con la evaluación de los Execution Reports
                //else
                //{
                //    DoLog(string.Format("{0}: Unknown message:{1} ", PrimaryConfiguration.Name, value.ToString()), Constants.MessageType.Information);
                //}
            }
        }

        public void onCreate(SessionID value)
        {
            lock (tLock)
            {
                DoLog(string.Format("@{0}-Invocación de onCreate : {1}",PrimaryConfiguration.Name, value.ToString()), 
                     Constants.MessageType.Information);
            }
        }

        public void onLogon(SessionID value)
        {
            lock (tLock)
            {
                SessionID = value;
                DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

                if (SessionID != null)
                    DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);
                else
                    DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);
            }
        }

        public void onLogout(SessionID value)
        {
            lock (tLock)
            {
                SessionID = null;
                DoLog("Invocación de onLogout : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void toAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                if (value is QuickFixT11.Logon)
                {
                    QuickFixT11.Logon logon = (QuickFixT11.Logon)value;
                    logon.setField(Username.FIELD, PrimaryConfiguration.User);
                    logon.setField(Password.FIELD, PrimaryConfiguration.Password);
                    DoLog(string.Format("@{0}:Invocación de toAdmin-logon por la sesión {1}:{2}", PrimaryConfiguration.Name, sessionId.ToString(), value.ToString()), Constants.MessageType.Information);
                }
                else if (value is QuickFixT11.Reject)
                {
                    QuickFixT11.Reject reject = (QuickFixT11.Reject)value;
                    DoLog(string.Format("@{0}:Invocación de toAdmin-reject por la sesión {1}:{2}", PrimaryConfiguration.Name, sessionId.ToString(), value.ToString()), Constants.MessageType.Information);
                }
                else
                    DoLog(string.Format("@{0}:Invocación de toAdmin por la sesión {1}:{2}", PrimaryConfiguration.Name, sessionId.ToString(), value.ToString()), Constants.MessageType.Information);
            }
        }

        public void toApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog(string.Format("@{0}:Invocación de toApp por la sesión {1}:{2}", PrimaryConfiguration.Name, sessionId.ToString(), value.ToString()), Constants.MessageType.Information);
            }
        }

        #endregion
    }
}

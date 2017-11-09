using QuickFix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.SecurityListMarketClient.Primary.Common.Wrappers;

namespace zHFT.SecurityListMarketClient.Primary.Client
{
    public class PrimarySecurityListMarketClient : BaseCommunicationModule, Application
    {
        #region Private Consts

        private string _DUMMY_SECURITY = "kcdlsncslkd";

        #endregion

        #region Private/Protected Attributes

        protected SecurityListMarketClient.Primary.Common.Configuration.Configuration PrimaryConfiguration
        {
            get { return (SecurityListMarketClient.Primary.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected SessionSettings SessionSettings { get; set; }
        protected FileStoreFactory FileStoreFactory { get; set; }
        protected ScreenLogFactory ScreenLogFactory { get; set; }
        protected SessionID SessionID { get; set; }
        protected MessageFactory MessageFactory { get; set; }
        protected SocketInitiator Initiator { get; set; }


        #endregion

        #region Public Overriden Methods

        public override Main.Common.DTO.CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    DoLog("Sending message " + action + " not implemented", Main.Common.Util.Constants.MessageType.Information);

                    return CMState.BuildFail(new Exception("Sending message " + action + " not implemented"));
                }
                else
                    throw new Exception("Invalid Wrapper");


            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Main.Common.Util.Constants.MessageType.Error);
                throw;
            }
        }

        public  override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string moduleConfigFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(moduleConfigFile))
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
                    DoLog("Error initializing config file " + moduleConfigFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + moduleConfigFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion

        #region Protected Methods

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new SecurityListMarketClient.Primary.Common.Configuration.Configuration().GetConfiguration<SecurityListMarketClient.Primary.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion

        #region Constructors

        public PrimarySecurityListMarketClient() { }

        #endregion

        #region QuickFix Methods

        public void fromAdmin(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de fromAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void fromApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                try
                {
                    DoLog("Invocación de fromApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

                    if (value is QuickFix50.SecurityList)
                    {
                        SecurityListWrapper wrapper = new SecurityListWrapper((QuickFix50.SecurityList)value, (IConfiguration)Config);

                        CMState state = OnMessageRcv(wrapper);

                        if (state.Success)
                            DoLog(string.Format("Primary Publishing Security List "), Main.Common.Util.Constants.MessageType.Information);
                        else
                            DoLog(string.Format("Error Publishing Security List. Error={0} ",
                                                state.Exception != null ? state.Exception.Message : ""),
                                                Main.Common.Util.Constants.MessageType.Error);

                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Critical Error @fromApp. Error={1} ",
                                                    PrimaryConfiguration.Name,
                                                    ex.Message),
                                                    Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        public void onCreate(SessionID value)
        {
            lock (tLock)
            {
                DoLog("Invocación de onCreate : " + value.ToString(), Constants.MessageType.Information);
            }
        }

        public void onLogon(SessionID value)
        {
            lock (tLock)
            {
                try
                {
                    SessionID = value;
                    DoLog("Invocación de onLogon : " + value.ToString(), Constants.MessageType.Information);

                    if (SessionID != null)
                    {
                        DoLog(string.Format("Logged for SessionId : {0}", value.ToString()), Constants.MessageType.Information);
                        QuickFix50Sp2.SecurityListRequest rq = new QuickFix50Sp2.SecurityListRequest();

                        rq.setInt(QuickFix.SecurityListRequestType.FIELD, PrimaryConfiguration.SecurityListRequestType);
                        rq.setString(SecurityReqID.FIELD, _DUMMY_SECURITY);

                        Session.sendToTarget(rq, SessionID);

                    }
                    else
                    {
                        DoLog("Error logging to FIX Session! : " + value.ToString(), Constants.MessageType.Error);
                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Critical Error @onLogon. Error={1} ",
                                        PrimaryConfiguration.Name,
                                        ex.Message),
                                        Main.Common.Util.Constants.MessageType.Error);
                }
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
                try
                {
                    if (value is QuickFixT11.Logon)
                    {
                        QuickFixT11.Logon logon = (QuickFixT11.Logon)value;
                        logon.setField(Username.FIELD, PrimaryConfiguration.User);
                        logon.setField(Password.FIELD, PrimaryConfiguration.Password);
                        DoLog("Invocación de toAdmin-logon por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                    }
                    else if (value is QuickFixT11.Reject)
                    {
                        QuickFixT11.Reject reject = (QuickFixT11.Reject)value;
                        DoLog("Invocación de toAdmin-reject por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                    }
                    else
                        DoLog("Invocación de toAdmin por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Critical Error @toAdmin. Error={1} ",
                                        PrimaryConfiguration.Name,
                                        ex.Message),
                                        Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        public void toApp(Message value, SessionID sessionId)
        {
            lock (tLock)
            {
                DoLog("Invocación de toApp por la sesión " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
            }
        }

        #endregion
    }
}

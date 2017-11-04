using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Configuration;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Quickfix.Common;

namespace zHFT.MarketClient.Quickfix.Client
{
    public class QuickfixClient : QuickfixMarketClientBase, ICommunicationModule
    {
        #region Private Attributes
        private SocketInitiator Initiator { get; set; }
        public IConfiguration Config { get; set; }
        #endregion

        #region Constructors

        public QuickfixClient() { }

        #endregion

        #region Protected Methods

        protected override IConfiguration GetConfig() { return Config; }

        protected void SetupServerSocket()
        {
            try
            {
                //TO DO : Activar configuracion FIX
                //SessionSettings = new SessionSettings(Config.FIXConfigFile);
                FileStoreFactory = new FileStoreFactory(SessionSettings);
                ScreenLogFactory = new ScreenLogFactory(SessionSettings);
                MessageFactory = new DefaultMessageFactory();

                Initiator = new SocketInitiator(this, FileStoreFactory, SessionSettings, ScreenLogFactory, MessageFactory);

                Initiator.start();
            }
            catch (Exception ex)
            {
                DoLog("Error initializing socket :" + ex.Message, Constants.MessageType.Error);
                throw;
            }
        }


        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Quickfix.Common.Configuration.Configuration().GetConfiguration<Quickfix.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion

        #region Public Methods

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string moduleConfigFile)
        {
            try
            {
                this.ModuleConfigFile = moduleConfigFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(moduleConfigFile))
                {
                    //TO DO: Cargr todos los converters de salida
                    //var type = Type.GetType(Config.NewOrderConverterAssembly);
                    //if (type != null)
                    //{
                    //    NewOrdenCoverter = (IConverter)Activator.CreateInstance(type, Config.ConverterConnectionString);
                    //}
                    //else
                    //{
                    //    DoLog("No se pudo crear la clase  " + Config.NewOrderConverterAssembly + "..", Constantes.TipoMensaje.Error);
                    //    return false;
                    //}

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + moduleConfigFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + moduleConfigFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    QuickFix.Message msg = null;
                    DoLog("Sending message " + action, Constants.MessageType.Information);

                    //TO DO: Implementar procesamiento de acciones para mensajes de salida
                    //if (action == Acciones.ALTA_ORDEN)
                    //{
                    //    msg = GetNewOrder(wrapper);
                    //}
                    //else
                    //    throw new Exception("Acción Inválida:" + action.ToString());

                    return DoSend(msg, action);
                }
                else
                    throw new Exception("Invalid Wrapper");


            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Constants.MessageType.Error);
                throw;
            }
        }

        #endregion

        #region Application Members

        public override void fromAdmin(Message value, SessionID sessionId)
        {
            DoLog("fromAdmin: " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
        }

        public override void fromApp(Message value, SessionID sessionId)
        {
            //TO DO: Implementar la lectura de market data
            //if (value is QuickFix50.NewOrderSingle)
            //    DoLog("Entrada de ordenes por el inititator no permitida:" + value.ToString(), Constantes.TipoMensaje.Error);
            //else if (value is QuickFix50.ExecutionReport)
            //    ProcessFixMessage(value, sessionId);
            //else if (value is QuickFix50.OrderCancelReject)
            //    ProcessFixMessage(value, sessionId);

            DoLog("fromApp: " + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
        }

        public override void onCreate(SessionID value)
        {
            DoLog("onCreate : " + value.ToString(), Constants.MessageType.Information);
        }

        public override void onLogon(SessionID value)
        {
            this.SessionID = value;
            DoLog("onLogon : " + value.ToString(), Constants.MessageType.Information);
        }

        public override void onLogout(SessionID value)
        {
            DoLog("onLogout : " + value.ToString(), Constants.MessageType.Information);
        }

        public override void toAdmin(Message value, SessionID sessionId)
        {
            DoLog("toAdmin:" + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);

        }

        public override void toApp(Message value, SessionID sessionId)
        {
            DoLog("toApp:" + sessionId.ToString() + ": " + value.ToString(), Constants.MessageType.Information);
        }

        #endregion

    }
}

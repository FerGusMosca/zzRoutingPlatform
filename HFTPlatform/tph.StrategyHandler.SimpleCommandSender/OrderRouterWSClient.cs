using System;
using System.Collections.Generic;
using tph.StrategyHandler.SimpleCommandSender.Common.Configuration;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.SingletonModulesHandler.Common.Interfaces;

namespace tph.StrategyHandler.SimpleCommandSender
{
    public class OrderRouterWSClient: BaseCommunicationModule, ILogger,ISingletonModule
    {
        
        #region Protected Attributs
        
        protected OnMessageReceived OnExecutionReportMessageRcv { get; set; }

        protected OnMessageReceived OnIncomingMessageRcv { get; set; }
        
        protected  static ISingletonModule Instance { get; set; }
        
        #endregion

        #region Constructors


        public OrderRouterWSClient(OnLogMessage pOnLogMsg, string configFile)
        {
            Initialize(null,pOnLogMsg, configFile);

        }


        #endregion

        #region Public Static Methods

        public static ISingletonModule GetInstance(OnLogMessage pOnLogMsg, string configFile)
        {
            if (Instance == null)
            {
                Instance = new OrderRouterWSClient(pOnLogMsg,configFile);
            }
            return Instance;
        }

        #endregion
        
        #region ICommunication Methods

        void ISingletonModule.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            DoLoadConfig(configFile, listaCamposSinValor);
        }

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        public void SetOutgoingEvent(OnMessageReceived OnMessageRcv)
        {
            OnExecutionReportMessageRcv += OnMessageRcv;
        }

        public void SetIncomingEvent(OnMessageReceived OnMessageRcv)
        {
            OnIncomingMessageRcv += OnMessageRcv;
        }

        void ILogger.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            DoLoadConfig(configFile, listaCamposSinValor);
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration().GetConfiguration<tph.StrategyHandler.SimpleCommandSender.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            if (wrapper.GetAction() == Actions.MARKET_DATA_REQUEST)
            {
                DoLog("Processing Market Data:" + wrapper.ToString(), Constants.MessageType.Information);
                //return ProcessMarketData(wrapper);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.NEW_ORDER)
            {
                //return OrderRouterModule.ProcessMessage(wrapper);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
            {
                //return OrderRouterModule.ProcessMessage(wrapper);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.UPDATE_ORDER)
            {
                //return OrderRouterModule.ProcessMessage(wrapper);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
            {
                //return OrderRouterModule.ProcessMessage(wrapper);
                return  CMState.BuildSuccess();
            }
            else if (wrapper.GetAction() == Actions.ORDER_MASS_STATUS_REQUEST)
            {
                //return OrderRouterModule.ProcessMessage(wrapper);
                return  CMState.BuildSuccess();
            }
            else
            {
                OnMessageRcv(wrapper);
                DoLog(string.Format("Invoking OnMessageRcv @SimpleCommandSender for action {0}",wrapper.GetAction()),Constants.MessageType.Error);
                return CMState.BuildSuccess();    
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                //this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    //Build the  trading modules
                    DoLog(string.Format("Initializing SimpleCommRcv module @{0}",DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")),Constants.MessageType.Information);
                    
                    //Finish starting up the server
                    //TODO Initialize Websocket client

                    DoLog("Websocket successfully initialized on URL:  " + Config, Constants.MessageType.Information);
                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critical error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }
        
        #endregion
    }
}
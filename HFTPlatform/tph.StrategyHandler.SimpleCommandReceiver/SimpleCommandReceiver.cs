using System;
using System.Collections.Generic;
using Fleck;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Configuration;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;

namespace tph.StrategyHandler.SimpleCommandReceiver
{
    public class SimpleCommandReceiver: BaseCommunicationModule
    {
        #region Protected Attributes
        
        protected WebSocketServer Server { get; set; }
        
        public tph.StrategyHandler.SimpleCommandReceiver.Common.Configuration.Configuration Config { get; set; }
        
        #endregion
        
        #region ICommunicationModule

        protected override void DoLoadConfig(string configFile, List<string> noValFields)
        {
            List<string> noValueFields = new List<string>();
            Config = new Configuration().GetConfiguration<tph.StrategyHandler.SimpleCommandReceiver.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            DoLog(string.Format("Invoking ProcessMessage @SimpleCommandReceiver which is not implemented"),Constants.MessageType.Error);
            return CMState.BuildSuccess();
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    //Finish starting up the server
//                    Server = new WebSocketServer(Configuration.WebSocketAdddress, this, OnMessageRcv);
//                    Server.Start();

                    DoLog("Websocket successfully initialized on URL:  " + Config.WebSocketURL, Constants.MessageType.Information);
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
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }
        
        #endregion

        
    }
}
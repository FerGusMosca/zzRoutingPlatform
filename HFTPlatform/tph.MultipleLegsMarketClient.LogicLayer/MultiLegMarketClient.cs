using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common;
using tph.MultipleLegsMarketClient.Common.Configuration;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Enums;

namespace tph.MultipleLegsMarketClient.LogicLayer
{
    public class MultiLegMarketClient : MarketClientBase, ICommunicationModule
    {

        #region Protected Attributes

        protected Configuration Configuration { get; set; }

        #endregion


        #region Protected Methods

        protected ICommunicationModule LoadModule(string module, string configFile, OnLogMessage pOnLogMsg)
        {
            DoLog($"Initializing {module} ", Constants.MessageType.Information);
            if (!string.IsNullOrEmpty(module))
            {
                var typeModule = Type.GetType(module);
                if (typeModule != null)
                {
                    ICommunicationModule dest = (ICommunicationModule)Activator.CreateInstance(typeModule);
                    dest.Initialize(ProcessMarketClientMessage, pOnLogMsg, configFile);
                    return dest;
                }
                else
                    throw new Exception("assembly not found: " + module);
            }
            else
            {
                DoLog($"{module} not found. It will not be initialized", Constants.MessageType.Error);
                return null;
            }
        }


        protected void InitializeModules()
        {
            foreach (MarketClient marketClient in Configuration.MarketCLients)
            {
                try
                {

                    ICommunicationModule commModule = LoadModule(marketClient.IncomingModule, marketClient.IncomingConfigPath, DoLog);
                    //TODO Asignar correctamente los incoming modules

                }
                catch (Exception ex)
                {
                    throw new Exception($"@{Configuration.Name}- CRTICIAL error instantiating module {marketClient.IncomingModule}:{ex.Message}");
                
                }
            
            }
        
        }


        protected virtual CMState ProcessMarketClientMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog($"Incoming message from order routing w/ Action {wrapper.GetAction()}: " + wrapper.ToString(), Constants.MessageType.Information);

                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    //TODO Process Market Data
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST_REQUEST)
                {
                    //TODO Process Security List
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog($"Error processing message from market client: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        #endregion


        #region ICommunicationModule

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {

                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    //TODO all the Config evaluation to implement all the modules
                    //TODO initalize all the dictionaries
                    InitializeModules();

                    return true;
                }
                else
                {
                    DoLog($"Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }

        public CMState ProcessMessage(Wrapper wrapper)
        {
            //TODO implement incoming messages from outside world
            return CMState.BuildSuccess();
        }


        #endregion


        #region Abstract Methods

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            Configuration = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        protected override IConfiguration GetConfig()
        {
            return Configuration;
        }


        #endregion
    }
}

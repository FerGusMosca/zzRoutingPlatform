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

namespace tph.MultipleLegsMarketClient.LogicLayer
{
    public class MultiLegMarketClient : MarketClientBase, ICommunicationModule
    {

        #region Protected Attributes

        protected Configuration Configuration { get; set; }

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

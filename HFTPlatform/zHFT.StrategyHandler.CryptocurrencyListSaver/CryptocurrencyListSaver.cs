using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver
{
    public class CryptocurrencyListSaver : BaseCommunicationModule
    {
        #region Protected Attributes

        protected zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration SecurityListSaverConfiguration
        {
            get { return (zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        protected DateTime Start { get; set; }

        #endregion

        #region Protected Methods
        
        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration().GetConfiguration<zHFT.StrategyHandler.CryptocurrencyListSaver.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected CMState ProcessSecurityList(Wrapper wrapper)
        {
            try
            {
                lock (tLock)
                {
                    //Sacar las cryptos y guardarlas
                    return CMState.BuildSuccess();
                }
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        #endregion

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    DoLog("Processing Security List:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    return ProcessSecurityList(wrapper);
                }
                //else if (wrapper.GetAction() == Actions.MARKET_DATA)
                //{
                //    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                //    return ProcessMarketData(wrapper);
                //}
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), SecurityListSaverConfiguration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + SecurityListSaverConfiguration.Name + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public override bool Initialize(Main.Common.Interfaces.OnMessageReceived pOnMessageRcv, Main.Common.Interfaces.OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    Start = DateTime.Now;

                    //Ya arrancamos pidiendo el security list
                    SecurityListRequestWrapper wrapper = new SecurityListRequestWrapper(SecurityListRequestType.AllSecurities, null);
                    OnMessageRcv(wrapper);

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }
    }
}

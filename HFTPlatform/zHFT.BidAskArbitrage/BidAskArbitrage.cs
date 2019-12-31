using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;

namespace zHFT.BidAskArbitrage
{
    public class BidAskArbitrage : ICommunicationModule,ILogger
    {

        #region Protected Attributes

        protected Thread RequestMarketDataThread { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected IConfiguration Config { get; set; }

        protected zHFT.BidAskArbitrage.Common.Configuration.Configuration Configuration
        {
            get { return (zHFT.BidAskArbitrage.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        #endregion

        #region ICommunicationModule

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);

                    return ProcessMarketData(wrapper);
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Configuration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + Configuration.Name + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (ConfigLoader.LoadConfig(this, configFile))
                {
                    RequestMarketDataThread = new Thread(LoadMonitorsAndRequestMarketData);
                    RequestMarketDataThread.Start();
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

        #region Protected Methods

        protected CMState ProcessMarketData(Wrapper wrapper)
        {
            //TODO: Implementar los cálculos
            return CMState.BuildSuccess();
        }

        protected void LoadMonitorsAndRequestMarketData()
        {
            Thread.Sleep(3000);
            foreach (string pair in Configuration.PairsToMonitor)
            {
                string[] symbols = pair.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                if (symbols.Length == 2)
                {

                    //TODO: Crear estructura de monitoreo de sec1 y sec2

                    //Security sec1 = new Security() { Symbol = symbols[0], Exchange = Configuration.Exchange, Currency = Configuration.Currency, SecType = SecurityType.TBOND };
                    //MarketDataRequestWrapper mdrWrapper1 = new MarketDataRequestWrapper(sec1, SubscriptionRequestType.SnapshotAndUpdates);
                    //OnMessageRcv(mdrWrapper1);

                    Security sec2 = new Security() { Symbol = symbols[1], Exchange = Configuration.Exchange, Currency = Configuration.Currency, SecType = SecurityType.TBOND };
                    MarketDataRequestWrapper mdrWrapper2 = new MarketDataRequestWrapper(sec2, SubscriptionRequestType.SnapshotAndUpdates);
                    OnMessageRcv(mdrWrapper2);

                    DoLog(string.Format("Pidiendo monitorieo de par {0}", pair), Constants.MessageType.Information);
                }
                else
                    DoLog(string.Format("No se puede monitorear al pair {0}", pair), Constants.MessageType.Error);
            
            }
        }

        

        #endregion

        #region Public Methods

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
            {
                if (Configuration != null)
                    OnLogMsg(string.Format("{0}:{1}", Configuration.Name, msg), type);
                else
                    OnLogMsg(string.Format("{0}:{1}", "BidAskArbitrage", msg), type);
            }
        }

        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Configuration = new zHFT.BidAskArbitrage.Common.Configuration.Configuration().GetConfiguration<zHFT.BidAskArbitrage.Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion


    }
}

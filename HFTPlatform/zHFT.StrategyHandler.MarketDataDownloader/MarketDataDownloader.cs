using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.MktDataDownloader.Common.Configuration;
using zHFT.StrategyHandler.MktDataDownloader.Common.Util;
using zHFT.StrategyHandler.MktDataDownloader.DAL;

namespace zHFT.StrategyHandler.MarketDataDownloader
{
    public class MarketDataDownloader : BaseCommunicationModule
    {
        #region Protected Attributes

        protected ADOBondMarketData ADOBondMarketData { get; set; }

        protected ICommunicationModule MarketDataModule { get; set; }

        protected Configuration Configuration
        {
            get { return (Configuration)Config; }
            set { Config = value; }
        }


        #endregion

        #region protected Methods

        protected void DoLog(string msg, zHFT.Main.Common.Util.Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        protected void ProcessMarketData(object param)
        { 
            //TODO: Guardar el market data
        }

        //To Process Order Routing Module messages
        protected CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog("Incoming message from Market Data Module: " + wrapper.ToString(), zHFT.Main.Common.Util.Constants.MessageType.Information);

                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    Thread ProcessMarketDataThread = new Thread(new ParameterizedThreadStart(ProcessMarketData));
                    ProcessMarketDataThread.Start(wrapper);
                }
                else
                {
                    throw new Exception(string.Format("Action not implemented: {0} @MarketDataDownloader", wrapper.GetAction()));
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, zHFT.Main.Common.Util.Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        protected void RequestMarketData(object param)
        {

            try
            {
                //1- We validate date range
                DateTime from = TimeParser.GetTodayDateTime(Configuration.MarketStartTime);
                DateTime to = TimeParser.GetTodayDateTime(Configuration.MarketEndTime);

                //2- Implementar todos los market data request

            }
            catch (Exception ex)
            {
                DoLog("Error Requesting market data " + ex.Message, zHFT.Main.Common.Util.Constants.MessageType.Error);
            }
        }

        #endregion


        #region BaseCommunicationModule

        protected override void DoLoadConfig(string configFile, List<string> fields)
        {
            Config = new Configuration().GetConfiguration<Configuration>(configFile, fields);
        }

        public override Main.Common.DTO.CMState ProcessMessage(Main.Common.Wrappers.Wrapper wrapper)
        {
            throw new NotImplementedException();
        }

        public override bool Initialize(Main.Common.Interfaces.OnMessageReceived pOnMessageRcv, Main.Common.Interfaces.OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    DoLog("Initializing MarketDataDownloader ", zHFT.Main.Common.Util.Constants.MessageType.Information);

                    if (!string.IsNullOrEmpty(Configuration.IncomingModule))
                    {
                        var typeMarketDataModule = Type.GetType(Configuration.IncomingModule);
                        if (typeMarketDataModule != null)
                        {
                            MarketDataModule = (ICommunicationModule)Activator.CreateInstance(typeMarketDataModule);
                            MarketDataModule.Initialize(ProcessOutgoing, pOnLogMsg, Configuration.IncomingConfigPath);
                        }
                        else
                            throw new Exception("assembly not found: " + Configuration.IncomingModule);
                    }
                    else
                        DoLog("Order Router not found. It will not be initialized", zHFT.Main.Common.Util.Constants.MessageType.Error);


                    ADOBondMarketData = new ADOBondMarketData(Configuration.ConnectionString);

                    tLock = new object();

                    Thread MarketDataRequestThread = new Thread(RequestMarketData);
                    MarketDataRequestThread.Start();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, zHFT.Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, zHFT.Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}

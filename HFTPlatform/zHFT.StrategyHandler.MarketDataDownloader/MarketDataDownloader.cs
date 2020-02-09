using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.MktDataDownloader.BE;
using zHFT.StrategyHandler.MktDataDownloader.Common.Configuration;
using zHFT.StrategyHandler.MktDataDownloader.Common.Util;
using zHFT.StrategyHandler.MktDataDownloader.DAL;

namespace zHFT.StrategyHandler.MarketDataDownloader
{
    public class MarketDataDownloader : BaseCommunicationModule
    {
        #region Protected Static Consts

        protected static string _BOND_SEC_TYPE = "B";

        #endregion


        #region Protected Attributes

        protected ADOBondManager ADOBondManager { get; set; }

        protected ADOBondMarketDataManager ADOBondMarketData { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected ICommunicationModule MarketDataModule { get; set; }

        protected int MarketDataRequestCounter { get; set; }

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

      

        //To Process Order Routing Module messages
        protected CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog("Incoming message from Market Data Module: " + wrapper.ToString(), zHFT.Main.Common.Util.Constants.MessageType.Information);

                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    Thread ProcessMarketDataThread = new Thread(new ParameterizedThreadStart(DoProcessMarketData));
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

        protected void DoProcessMarketData(object param)
        {
            try
            {
                Wrapper mdWrapper = (Wrapper)param;

                //1- We validate date range
                DateTime from = TimeParser.GetTodayDateTime(Configuration.MarketStartTime);
                DateTime to = TimeParser.GetTodayDateTime(Configuration.MarketEndTime);

                if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday
                    && DateTime.Compare(from, DateTime.Now) < 0 && DateTime.Compare(DateTime.Now, to) < 0)
                {
                    //1-Convert Market Data
                    MarketData md =  MarketDataConverter.GetMarketData(mdWrapper, Configuration);

                    if (md.Security.SecType == SecurityType.TBOND && Configuration.SecurityType == _BOND_SEC_TYPE)
                    {

                        BondMarketData bondMarketData = new BondMarketData()
                        {
                            BestAskPrice = md.BestAskPrice.HasValue ? Convert.ToDecimal(md.BestAskPrice.Value) : 0,
                            BestAskSize = md.BestAskSize.HasValue ? Convert.ToDecimal(md.BestAskSize.Value) : 0,
                            BestBidPrice = md.BestBidPrice.HasValue ? Convert.ToDecimal(md.BestBidPrice.Value) : 0,
                            BestBidSize = md.BestBidSize.HasValue ? Convert.ToDecimal(md.BestBidSize.Value) : 0,
                            //Datetime = md.MDEntryDate.HasValue ? md.MDEntryDate.Value : DateTime.Now.Date,
                            Datetime=DateTime.Now,
                            LastTrade = md.Trade.HasValue ? Convert.ToDecimal(md.Trade.Value) : 0,
                            Symbol = md.Security.Symbol,
                            SettlDate=md.SettlType.ToString(),
                            Timestamp = DateTime.Now.ToString("yyyyMMddhhmmss")

                        };

                        //2- Persist Market Data
                        ADOBondMarketData.PersistBondMarketData(bondMarketData);

                    }
                    else
                    {
                        DoLog(string.Format("Received market data for not processed security type: {0}", md.Security.SecType.ToString()), Main.Common.Util.Constants.MessageType.Error);
                    }
                  
                }
            }
            catch (Exception ex)
            {
                DoLog("Error Requesting market data " + ex.Message, zHFT.Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void RequestMarketData(object param)
        {
            try
            {
              

                List<Bond> bonds =  ADOBondManager.GetBonds("BUE");

                //1- Requesting Market Data for every bond
                foreach (Bond bond in bonds)
                {
                    Security bondToRequest = new Security();
                    bondToRequest.Symbol = bond.Symbol;
                    bondToRequest.Exchange = bond.Market;
                    bondToRequest.SecType = SecurityType.TBOND;
                    bondToRequest.MarketData = new MarketData() { SettlType = SettlType.Tplus2 };

                    MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, bondToRequest, SubscriptionRequestType.SnapshotAndUpdates);
                    MarketDataRequestCounter++;
                    MarketDataModule.ProcessMessage(wrapper);
                }
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
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog("Processing Market Data:" + wrapper.ToString(), Main.Common.Util.Constants.MessageType.Information);
                    Thread doProcessMDThread = new Thread(DoProcessMarketData);
                    doProcessMDThread.Start(wrapper);

                    return CMState.BuildSuccess();
                }
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Configuration.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @MarketDataDownloader"+ ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
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


                    ADOBondMarketData = new ADOBondMarketDataManager(Configuration.ConnectionString);

                    ADOBondManager = new ADOBondManager(Configuration.ConnectionString);

                    MarketDataConverter = new MarketDataConverter();

                    tLock = new object();

                    Thread MarketDataRequestThread = new Thread(RequestMarketData);
                    MarketDataRequestThread.Start();

                    MarketDataRequestCounter++;

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

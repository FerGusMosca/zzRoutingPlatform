using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Configuration;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.DataAccessLayer;
using static zHFT.Main.Common.Util.Constants;

namespace tph.StrategyHandler.CandleDownloader.LogicLayer
{
    public class CandleDownloader : ICommunicationModule, ILogger
    {

        #region Protected Attributes

        protected tph.StrategyHandler.CandleDownloader.Common.Configuration.Configuration Config { get; set; }

        public string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected CandleManager CandleManager { get; set; }

        protected Dictionary<string, MonTurtlePosition> MonitorPositions { get; set; }

        protected   int MarketDataRequestCounter { get; set; }

        protected object tLock { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        #endregion

        #region ILogger
        public void DoLoadConfig(string configFile, List<string> valFields)
        {
            Config = ConfigLoader.GetConfiguration<tph.StrategyHandler.CandleDownloader.Common.Configuration.Configuration>(this, configFile, valFields);
        }

        public void DoLog(string msg, Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(string.Format("{0}", msg), type);
        }

        #endregion

        #region Protected Methods

        protected void ProcessMarketDataThread(object param)
        {
            try
            { 
                Wrapper mdWrapper=  (Wrapper)param;

                lock (tLock)
                {

                    
                    MarketData md = MarketDataConverter.GetMarketData(mdWrapper, Config);

                    if (MonitorPositions.ContainsKey(md.Security.Symbol))
                    {
                        MonTurtlePosition monPos = MonitorPositions[md.Security.Symbol];
                        bool newCandle = monPos.AppendCandle(md);

                        if (newCandle) {

                            
                            MarketData candleToPersist = monPos.GetLastFinishedCandle();

                            if (candleToPersist != null)
                            {
                                DoLog($"{Config.Name}- Persistign Candle :{md.ToString()}", MessageType.Information);

                                string symbol = md.Security.Symbol;
                                if (Config.PersistWithFullSymbol) {

                                    symbol = md.Security.Symbol + "." + md.Security.Exchange;
                                }

                                CandleManager.Persist(symbol, CandleInterval.Minute_1, candleToPersist);
                                DoLog($"{Config.Name}-  Candle for symbol {md.Security.Symbol} successfully persisted", MessageType.Information);
                            }
                        }

                    }
                    else
                        DoLog($"{Config.Name}-Ignoring unknwon symbol {md.Security.Symbol}", MessageType.Information);
                }
            
            }
            catch(Exception ex)
            {
                DoLog($"ERROR Processing Market Data:{ex.Message}", MessageType.Error);
            }
        
        }

        protected void DoRequestMarketDataThread(object param)
        {
            try
            {
                Thread.Sleep(3000);
                foreach (string symbol in Config.SymbolsToDownload)
                {
                    DoLog($"{Config.Name}- Requesting Candles for symbol {symbol}", MessageType.Information);
                    if (!MonitorPositions.ContainsKey(symbol))
                    {
                        Security sec = new Security()
                        {
                            Symbol = symbol,
                            SecType = Security.GetSecurityType(Config.SecurityType),
                            MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
                            Currency = Config.Currency,
                            Exchange = Config.Exchange
                        };

                        MonTurtlePosition portfPos = new MonTurtlePosition(
                            null,
                            0,
                            Config.CandleReferencePrice)
                        {
                            Security = sec,
                            DecimalRounding = 0,
                        };

                        //1- We add the current security to monitor
                        MonitorPositions.Add(symbol, portfPos);


                        //2- We request market data

                        MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
                        MarketDataRequestCounter++;
                        OnMessageRcv(wrapper);
                        DoLog($"{Config.Name}- Candles for symbol {symbol} successfully requested", MessageType.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL ERROR Requesting Market Data:{ex.Message}", MessageType.Error);
            
            }

        }

        #endregion


        #region ICommunicationModule
        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;


            if (ConfigLoader.LoadConfig(this, configFile))
            {
                
                CandleManager = new CandleManager(Config.ConnectionString);
                MonitorPositions = new Dictionary<string, MonTurtlePosition>();
                MarketDataConverter = new MarketDataConverter();
                tLock = new object();

                MarketDataRequestCounter = 0;

                Thread reqMarketDataThread = new Thread(new ParameterizedThreadStart(DoRequestMarketDataThread));
                reqMarketDataThread.Start();

                return true;

            }
            else
            {
                return false;
            }
        }

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    DoLog($"{Config.Name}-->Recv Market Data :{wrapper.ToString()}", MessageType.Information);
                    Thread reqMarketDataThread = new Thread(new ParameterizedThreadStart(ProcessMarketDataThread));
                    reqMarketDataThread.Start(wrapper);
                    return CMState.BuildSuccess();
                }

                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Config.Name)));
            }
            catch (Exception ex)
            {
                DoLog($"ERROR @ProcessMessage {Config.Name} : {ex.Message}", MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion
    }
}

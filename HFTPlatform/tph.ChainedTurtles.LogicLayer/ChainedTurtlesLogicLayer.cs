using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ChainedTurtles.BusinessEntities;
using tph.ChainedTurtles.Common;
using tph.ChainedTurtles.Common.Configuration;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Configuration;
using tph.DayTurtles.LogicLayer;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.LogicLayer.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.DTO;
using zHFT.StrategyHandler.Common.Wrappers;

namespace tph.ChainedTurtles.LogicLayer
{
    public class ChainedTurtlesLogicLayer : tph.TrendlineTurtles.LogicLayer.TrendlineTurtles
    {

        #region Protected Attributes

        public Dictionary<string, MonTurtlePosition> ChainedIndicators { get; set; }

        #endregion

        #region Public Overriden Methods

        public override void DoLoadConfig(string configFile, List<string> noValFlds)
        {
            Config = ConfigLoader.GetConfiguration<ChainedConfiguration>(this, configFile, noValFlds);
        }

        protected override void LoadMonitorsAndRequestMarketData()
        {
            try
            {
                LoadMonitorIndicators();

                lock (Config)
                {
                    foreach (var security in GetConfig().SecuritiesToMonitor)
                    {

                        //#1- Load monitors for trading securites
                        if (!MonitorPositions.ContainsKey(security.Symbol))
                        {
                            Security sec = new Security()
                            {
                                Symbol = security.Symbol,
                                SecType = Security.GetSecurityType(security.SecurityType),
                                MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
                                Currency = security.Currency,
                                Exchange = security.Exchange
                            };

                            MonChainedTurtlePosition monChPos = new MonChainedTurtlePosition(
                                sec, GetCustomConfig(security.Symbol),
                                GetConfig().StopLossForOpenPositionPct,
                                GetConfig().CandleReferencePrice);

                            //1- We add the current security to monitor
                            MonitorPositions.Add(security.Symbol, monChPos);

                            Securities.Add(sec);//So far, this is all wehave regarding the Securities

                            //2- Load all the indicators pre loaded for the newly monitored security
                            foreach (var indicator in security.Indicators)
                            {
                                MonTurtlePosition innerIndicator = FetchIndicator(indicator.Code);
                                monChPos.AppendIndicator(innerIndicator);
                            }

                            //3- No market data to request until Historical Prices are recevied
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                DoLog($"@{Config.Name}--> CRITICAL ERROR loading monitors!: {ex.Message}", Constants.MessageType.Error);


            }


        }

        protected override void DoRequestHistoricalPricesThread(object param)
        {
            try
            {

                lock (Config)
                {
                    int i = 1;
                    foreach (var security in GetConfig().SecuritiesToMonitor)
                    {

                        DoLog($"@{GetConfig().Name}--> Requesting historical prices for monitored symbol {security.Symbol}", Constants.MessageType.Information);
                        
                        DoRequestHistoricalPrice(i, security.Symbol,
                                                GetCustomConfig(security.Symbol).OpenWindow,
                                                GetCustomConfig(security.Symbol).CloseWindow,
                                                security.Currency,
                                                SecurityTypeTranslator.TranslateNonMandatorySecurityType(security.SecurityType),
                                                security.Exchange
                                                );

                        i++;
                    }


                    foreach (var indicator in GetConfig().ChainedTurtleIndicators.Where(x => x.RequestPrices && x.SecurityToMonitor != null))
                    {

                        DoLog($"@{GetConfig().Name}--> Requesting historical prices for indicator {indicator.SecurityToMonitor.Symbol}", Constants.MessageType.Information);
                        DoRequestHistoricalPrice(i, indicator.SecurityToMonitor.Symbol,
                                                GetCustomConfig(indicator.SecurityToMonitor.Symbol).OpenWindow,
                                                GetCustomConfig(indicator.SecurityToMonitor.Symbol).CloseWindow,
                                                indicator.SecurityToMonitor.Currency,
                                                SecurityTypeTranslator.TranslateNonMandatorySecurityType(indicator.SecurityToMonitor.SecurityType),
                                                indicator.SecurityToMonitor.Exchange);
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"@{Config.Name}--> CRITICAL ERROR requesting historical prices: {ex.Message}", Constants.MessageType.Error);

            }
        }

        protected override async void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper)pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            OrderRouter.ProcessMessage(wrapper);


            try
            {
                lock (tLock)
                {
                    DateTimeManager.NullNow = md.GetReferenceDateTime();

                    if (MonitorPositions.ContainsKey(md.Security.Symbol) && Securities != null
                                                                                    && ProcessedHistoricalPrices
                                                                                        .Contains(md.Security.Symbol))
                    {
                        MonTrendlineTurtlesPosition monPos = (MonTrendlineTurtlesPosition)MonitorPositions[md.Security.Symbol];
                        if (monPos.HasHistoricalCandles())
                        {
                            bool newCandle = monPos.AppendCandle(md);

                            EvalOpeningClosingPositions(monPos);//We will see the inner indicatros if they are on
                            UpdateLastPrice(monPos, md);
                        }
                    }
                    else if (ChainedIndicators.ContainsKey(md.Security.Symbol))
                    {
                        MonTurtlePosition indicator = ChainedIndicators[md.Security.Symbol];
                        bool newCandle = indicator.AppendCandle(md);
                        EvalMarketDataCalculations(indicator, md);
                    }
                    else
                    { 
                        //Non tracked symbol
                    }
                }

            }
            catch (Exception e)
            {
                DoLog(
                    string.Format("ERROR @DailyTurtles- Error processing market data:{0}-{1}", e.Message, e.StackTrace),
                    Constants.MessageType.Error);
            }
        }

        #endregion

        #region Protected/Private Methods
        protected virtual ChainedConfiguration GetConfig()
        {
            return (ChainedConfiguration)Config;
        }

        private void InitializeMonitorIndicator(ChainedTurtleIndicator indicator, Security sec)
        {

            var indType = Type.GetType(indicator.Assembly);
            if (indType != null)
            {
                MonTurtlePosition monInd = (MonTurtlePosition)Activator.CreateInstance(indType, new object[]
                                                                                    {
                                                                                                sec,
                                                                                                GetCustomConfig(indicator.SecurityToMonitor.Symbol),
                                                                                                GetConfig().CandleReferencePrice,
                                                                                                indicator.SignalType,
                                                                                                indicator.RequestPrices
                                                                                    }
                                                                                    );


                if (!ChainedIndicators.ContainsKey(indicator.Code))
                    ChainedIndicators.Add(indicator.Code, monInd);
                else
                    throw new Exception($"Duplcated asembly indicator for indicator code {indicator.Code}");


            }
            else
                throw new Exception($"Assembly not found: {indicator.Assembly} ");
        }

        private void LoadMonitorIndicators()
        {
            try
            {


                lock (Config)
                {
                    foreach (var indicator in GetConfig().ChainedTurtleIndicators)
                    {

                        //This security works as an inner indicator of another trading security
                        Security ind = new Security()
                        {
                            Symbol = indicator.SecurityToMonitor.Symbol,
                            SecType = Security.GetSecurityType(indicator.SecurityToMonitor.SecurityType),
                            MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
                            Currency = indicator.SecurityToMonitor.Currency,
                            Exchange = indicator.SecurityToMonitor.Exchange
                        };


                        InitializeMonitorIndicator(indicator, ind);
                    }
                }


            }
            catch (Exception ex)
            {
                DoLog($"@{GetConfig().Name}--> CRITITAL ERROR initializing monitor indicators:{ex.Message}", Constants.MessageType.Error);

            }

        }

        private MonTurtlePosition FetchIndicator(string code)
        {

            if (ChainedIndicators.ContainsKey(code))
            {
                return ChainedIndicators[code];
            }
            else
                throw new Exception($"Could not find a pre loaded indicator for code {code}!");


        }

        protected void InitializeIndicators(OnLogMessage pOnLogMsg)
        {
            foreach (var indicator in ChainedIndicators.Values)
            {

                TrendLineCreator.InitializeCreator(indicator.Security,
                                                   GetConfig(),
                                                   DateTimeManager.Now.AddDays(GetConfig().HistoricalPricesPeriod),
                                                   pOnLogMsg);

                DoLog($"Portfolio Position for indicator {indicator.Security.Symbol} successfully initialized",
                    Constants.MessageType.Information);
            }
        }

        protected void EvalHistoricalPricesPrecalculations(MonitoringPosition monPos)
        {
            if (monPos.IsTrendlineMonPosition())
            {
                DoLog($"Buiding trendlines for indicator {monPos.Security.Symbol}", Constants.MessageType.Information);
                BuildTrendlines((MonTrendlineTurtlesPosition)monPos);
            }
        }

        protected void EvalMarketDataCalculations(MonitoringPosition indicator,MarketData md)
        {
            bool newCandle = indicator.AppendCandle(md);

            if (indicator.IsTrendlineMonPosition())
            {
                if (newCandle)
                {
                    RecalculateNewTrendlines((MonTrendlineTurtlesPosition)indicator, GetConfig().RecalculateTrendlines);
                    indicator.EvalSignalTriggered();
                    EvalBrokenTrendlines((MonTrendlineTurtlesPosition)indicator, md);
                }

            }
        }

        protected void DoRequestMarketData(MonTurtlePosition monPos)
        {
            DoLog($"Requesting market data for security/indicator {monPos.Security.Symbol}", Constants.MessageType.Information);
            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, monPos.Security,
                                                                            SubscriptionRequestType.SnapshotAndUpdates);
            MarketDataRequestCounter++;
            OnMessageRcv(wrapper);
        }

        protected override void ProcessHistoricalPrices(object pWrapper)
        {
            try
            {
                HistoricalPricesDTO dto = null;

                dto = LoadHistoricalPrices((HistoricalPricesWrapper)pWrapper);


                if (dto != null && dto.Symbol != null)
                {
                    lock (tSynchronizationLock)
                    {
                        if (MonitorPositions.ContainsKey(dto.Symbol))
                        {
                            MonTurtlePosition monPos = (MonTurtlePosition)MonitorPositions[dto.Symbol];
                            dto.MarketData.ForEach(x => monPos.AppendCandleHistorical(x));
                            DoRequestMarketData(monPos);
                            //If I am going to calculate the trendlines , it should be setup as an INDICATOR!!
                        }
                        else if (ChainedIndicators.Values.Any(x => x.Security.Symbol == dto.Symbol))
                        {
                            foreach (var indicator in ChainedIndicators.Values.Where(x => x.Security.Symbol == dto.Symbol))
                            {
                                dto.MarketData.ForEach(x => indicator.AppendCandleHistorical(x));
                                EvalHistoricalPricesPrecalculations(indicator);
                                DoRequestMarketData(indicator);
                            }

                        }
                        else
                            DoLog($"Could not find monitoring position for symbol {dto.Symbol} processing historical prices", Constants.MessageType.Debug);

                    }
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("Critical ERROR processing Trendlines : {0}", e.Message),
                    Constants.MessageType.Error);
            }


        }

        #endregion

        #region Public Overriden Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.ModuleConfigFile = configFile;
            //this.OnMessageRcv += pOnMessageRcv;
            //this.OnLogMsg += pOnLogMsg;
            StartTime = DateTimeManager.Now;
            LastCounterResetTime = StartTime;

            if (ConfigLoader.LoadConfig(this, configFile))
            {

                ChainedIndicators = new Dictionary<string, MonTurtlePosition>();
                ProcessedHistoricalPrices = new List<string>();

                LoadCustomTurtlesWindows();

                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

                InitializeIndicators(pOnLogMsg);

                InitializeManagers(GetConfig().ConnectionString);

                Thread depuarateThread = new Thread(EvalDepuratingPositionsThread);
                depuarateThread.Start();

                return true;

            }
            else
            {
                return false;
            }
        }


        #endregion

    }
}

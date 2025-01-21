using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ChainedTurtles.BusinessEntities;
using tph.ChainedTurtles.Common;
using tph.ChainedTurtles.Common.Configuration;
using tph.ChainedTurtles.Common.Interfaces;
using tph.ChainedTurtles.DataAccessLayer;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Configuration;
using tph.DayTurtles.LogicLayer;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.LogicLayer.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.DTO;
using zHFT.StrategyHandler.Common.Wrappers;
using static System.Collections.Specialized.BitVector32;

namespace tph.ChainedTurtles.LogicLayer
{
    public class ChainedTurtlesLogicLayer : tph.TrendlineTurtles.LogicLayer.TrendlineTurtles
    {

        #region Protected Attributes

        public Dictionary<string, MonTurtlePosition> ChainedIndicators { get; set; }

        protected TurtlesPortfolioPositionManager TurtlesPortfolioPositionManager { get; set; }

        protected int HistoricalPricesRequetsID { get; set; }

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
                    foreach (var secToMonitor in GetConfig().SecuritiesToMonitor)
                    {

                        //#1- Load monitors for trading securites
                        if (!MonitorPositions.ContainsKey(secToMonitor.Symbol))
                        {
                            Security sec = BuildFromConfigSecurityToMonitor(secToMonitor);

                            MonChainedTurtlePosition monChPos = new MonChainedTurtlePosition(
                                sec, GetCustomConfig(secToMonitor.Symbol),
                                secToMonitor.GetMonitoringType(),
                                this);


                            //1- We add the current security to monitor
                            MonitorPositions.Add(secToMonitor.Symbol, monChPos);

                            Securities.Add(sec);//So far, this is all wehave regarding the Securities


                            if (monChPos.RequestHistoricalPrices)
                            {
                                Thread reqMonPosHistoricalPrices = new Thread(new ParameterizedThreadStart(DoRequestMonPosHistoricalPricesThread));
                                reqMonPosHistoricalPrices.Start();
                            }
                            else//we go straight for Market Data
                            {
                                if(!ProcessedHistoricalPrices.Contains(secToMonitor.Symbol))
                                    ProcessedHistoricalPrices.Add(monChPos.Security.Symbol);
                                DoRequestMarketData(monChPos);

                            }

                            //2- Load all the indicators pre loaded for the newly monitored security
                            foreach (var indicator in secToMonitor.Indicators)
                            {
                                MonChainedTurtleIndicator innerIndicator = FetchIndicator(indicator.Code);
                                monChPos.AppendIndicator(innerIndicator);
                            }


                        }
                    }

                    //3- No market data to request until Historical Prices are recevied
                    Thread reqIndicatorsHistoricalPrices = new Thread(new ParameterizedThreadStart(DoRequestIndicatorsHistoricalPricesThread));
                    reqIndicatorsHistoricalPrices.Start();

                    InitializeIndicators(DoLog);
                }

            }
            catch (Exception ex)
            {
                DoLog($"@{Config.Name}--> CRITICAL ERROR loading monitors!: {ex.Message}", Constants.MessageType.Error);


            }


        }

        protected override void DoRequestHistoricalPrice(int i,string symbol, int window, string currency,
                                               SecurityType? pSecurityType, string exchange)
        {




            DateTime from = DateTimeManager.Now.AddDays(window);
            DateTime to;
            if (Config.IsLiveMode())
                to = DateTime.Now.AddDays(1);//The last info that you have
            else
                to = MarketTimer.GetTodayDateTime(Config.OpeningTime);
            

            HistoricalPricesRequestWrapper reqWrapper = new HistoricalPricesRequestWrapper(i, symbol, from, to,
                                                                                            CandleInterval.Minute_1,
                                                                                            currency, pSecurityType, exchange);
            
            OnMessageRcv(reqWrapper);
        }

        protected void DoRequestMonPosHistoricalPricesThread(object param)
        {
            try
            {

                lock (Config)
                {
                    
                    foreach (var security in GetConfig().SecuritiesToMonitor)
                    {

                        DoLog($"@{GetConfig().Name}--> Requesting historical prices for monitored symbol {security.Symbol}", Constants.MessageType.Information);
                        var monPos = FetchMonitoringPosition(security.Symbol);
                        IMonPosition monPosEntity = GetMonPositionConfigInterface(monPos);
                        DoRequestHistoricalPrice(HistoricalPricesRequetsID, security.Symbol,
                                                monPosEntity.GetHistoricalPricesPeriod(),
                                                security.Currency,
                                                SecurityTypeTranslator.TranslateNonMandatorySecurityType(security.SecurityType),
                                                security.Exchange
                                                );
                        HistoricalPricesRequetsID++;
                    }
                   
                }
            }
            catch (Exception ex)
            {
                DoLog($"@{Config.Name}--> CRITICAL ERROR requesting historical prices for Monitoring Positions: {ex.Message}", Constants.MessageType.Error);

            }

        }

        protected void DoRequestHistoricalPrices(Security sec, int historicalPricesPeriod)
        {
            DoLog($"@{GetConfig().Name}--> Requesting historical prices for symbol {sec.Symbol}", Constants.MessageType.Information);
            DoRequestHistoricalPrice(HistoricalPricesRequetsID, sec.Symbol,
                                    historicalPricesPeriod,
                                    sec.Currency,
                                    sec.SecType,
                                    sec.Exchange);

            DoLog($"@{GetConfig().Name}-->  historical prices for symbol {sec.Symbol} successfully requested", Constants.MessageType.Information);

            HistoricalPricesRequetsID++;

        }

        protected void DoRequestIndicatorsHistoricalPricesThread(object param)
        {
            try
            {

                lock (Config)
                {
                    
                    foreach (MonChainedTurtleIndicator memInd in ChainedIndicators.Values)
                    {
                        if (memInd.RequestHistoricalPrices)
                        {

                            foreach (var sec in memInd.GetSecurities())
                            {
                                DoRequestHistoricalPrices(sec, GetTradingEntity(memInd).GetHistoricalPricesPeriod());
                            }
                            
                        }
                        else//we go straight for Market Data
                        {
                            foreach (var sec in memInd.GetSecurities())
                            {
                                if (!ProcessedHistoricalPrices.Contains(sec.Symbol))
                                    ProcessedHistoricalPrices.Add(sec.Symbol);
                                DoRequestMarketData(memInd);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"@{Config.Name}--> CRITICAL ERROR requesting historical prices: {ex.Message}", Constants.MessageType.Error);

            }
        }

        protected override void DoRequestHistoricalPricesThread(object param)
        {
            //Implemented on different threads
        }

        protected override async void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper)pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            OrderRouter.ProcessMessage(wrapper);

            Thread.Sleep(1000);

            try
            {

                DoLog($"@DBG_MD - Recv Market Data For Symbol {md.Security.Symbol}", Constants.MessageType.Debug);
                lock (tLock)
                {
                    if (!ValidateMarketDataRec(md)) { return; }

                    DateTimeManager.NullNow = md.GetReferenceDateTime();
                    bool monPosOrIndicatorFound = false;
                    if (MonitorPositions.ContainsKey(md.Security.Symbol) && Securities != null
                                                                                    && ProcessedHistoricalPrices
                                                                                        .Contains(md.Security.Symbol))
                    {
                        MonChainedTurtlePosition monPos = (MonChainedTurtlePosition)MonitorPositions[md.Security.Symbol];
                        DoLog($"@DBG_MD2 - Processing Market Data For MonPos {md.Security.Symbol} <HasHistoricalCandles={monPos.HasHistoricalCandles()} - RequestHistPrices={monPos.RequestHistoricalPrices}>", Constants.MessageType.Debug);
                        if (monPos.HasHistoricalCandles() || !monPos.RequestHistoricalPrices)
                        {
                            DoLog($"@DBG_MD3 - Eval Open/Close For MonPos {md.Security.Symbol}", Constants.MessageType.Debug);
                            bool newCandle = monPos.AppendCandle(md);
                            EvalOpeningClosingPositions(monPos);//We will see the inner indicatros if they are on
                            UpdateLastPrice(monPos, md);
                        }

                        monPosOrIndicatorFound = true;
                    }

                    if (ChainedIndicators.Values.Any(x => x.Security.Symbol == md.Security.Symbol))
                    {

                        DoLog($"@DBG_MD2 - Processing Market Data For Indicator {md.Security.Symbol}", Constants.MessageType.Debug);
                        foreach (var indicator in ChainedIndicators.Values.Where(x => x.Security.Symbol == md.Security.Symbol))
                        {
                            EvalMarketDataCalculations(indicator, md);
                        }

                        monPosOrIndicatorFound = true;
                    }

                    if(!monPosOrIndicatorFound)
                    {
                        //Non tracked symbol
                        DoLog($"Recv Market Data for Non Tracked Symbol {md.Security.Symbol}", Constants.MessageType.Information);
                    }
                }

            }
            catch (Exception e)
            {
                DoLog(
                    string.Format("ERROR @ChainedTurtlesLogicLayer- Error processing market data:{0}-{1}", e.Message, e.StackTrace),
                    Constants.MessageType.Error);
            }
        }

        #endregion

        #region Protected/Private Methods
        protected virtual ChainedConfiguration GetConfig()
        {
            return (ChainedConfiguration)Config;
        }

        private void InitializeMonitorIndicator(ChainedTurtleIndicator indicator)
        {

            var indType = Type.GetType(indicator.Assembly);
            if (indType != null)
            {
                MonTurtlePosition monInd = null;
                if (indicator.IsMultipleSecurity())
                {
                    List<Security> securities = new List<Security>();
                    indicator.SecuritiesToMonitor.ForEach(x => securities.Add(BuildFromConfigSecurityToMonitor(x)));

                    monInd = (MonTurtlePosition)Activator.CreateInstance(indType, new object[]
                                                                    {
                                                                                                securities,
                                                                                                GetCustomConfig(indicator.Code),
                                                                                                indicator.Code,
                                                                                                this
                                                                    }
                                                                    );
                    //The first security Monitoring Type is the indicator monitoring type (must all be the same)
                    monInd.MonitoringType = indicator.GetMonitorType();
                }
                else
                {
                    
                    Security sec = BuildFromConfigSecurityToMonitor(indicator.SecurityToMonitor);
                    monInd = (MonTurtlePosition)Activator.CreateInstance(indType, new object[]
                                                                                        {
                                                                                                sec,
                                                                                                GetCustomConfig(indicator.Code),
                                                                                                indicator.Code,
                                                                                                this
                                                                                        }
                                                                                        );

                    monInd.MonitoringType = indicator.SecurityToMonitor.GetMonitoringType();


                }

                

                if (!ChainedIndicators.ContainsKey(indicator.Code))
                    ChainedIndicators.Add(indicator.Code, monInd);
                else
                    throw new Exception($"Duplicated asembly indicator for indicator code {indicator.Code}");


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
                        //if (GetConfig().SecuritiesToMonitor.Any(x => indicator.IsMultipleSecurity() || x.Symbol == indicator.SecurityToMonitor.Symbol ))
                        //{


                            DoLog($"Initializing indicator {indicator.Code} in memory!", Constants.MessageType.Information);
                            InitializeMonitorIndicator(indicator);
                            DoLog($"Indicator {indicator.Code} successfully initialized!", Constants.MessageType.Information);
                        //}
                        //else
                        //{
                        //    DoLog($"Skipping initialization of indicator {indicator.Code} because there is not a security to monitor with symbol {indicator.SecurityToMonitor.Symbol}", Constants.MessageType.PriorityInformation);
                        
                        //}
                    }
                }


            }
            catch (Exception ex)
            {
                string msg = $"@{GetConfig().Name}--> CRITITAL ERROR initializing monitor indicators:{ex.Message}";
                //DoLog(msg, Constants.MessageType.Error);
                throw new Exception(msg);

            }

        }

        private MonitoringPosition FetchMonitoringPosition(string symbol)
        {

            if (MonitorPositions.ContainsKey(symbol))
                return MonitorPositions[symbol];
            else
                throw new Exception($"COULD NOT find a monitoring position for symbol {symbol}. Position not loaded in memory yet?");
        }

        private MonChainedTurtleIndicator FetchIndicator(string code)
        {
            
            if(ChainedIndicators.ContainsKey(code))
                return (MonChainedTurtleIndicator)ChainedIndicators[code];
            else
                throw new Exception($"Could not find a pre loaded indicator for code {code}!");
        }

        protected void InitializeIndicators(OnLogMessage pOnLogMsg)
        {
            foreach (var indicator in ChainedIndicators.Values)
            {

                if (indicator.IsTrendlineMonPosition())
                {
                    DoLog($"Initializing trendlines for indicator {indicator.Security.Symbol} ",Constants.MessageType.Information);

                    ITrendlineIndicator trndlInd = GetTrendlineIndicator(indicator);

                    TrendLineCreator.InitializeCreator(indicator.Security, trndlInd.GetPerforationThreshold(),
                                                       trndlInd.GetInnerTrendlinesSpan(), trndlInd.GetCandleReferencePrice(),
                                                       CandleInterval.Minute_1,
                                                       DateTimeManager.Now.AddDays(trndlInd.GetHistoricalPricesPeriod()),
                                                       pOnLogMsg, _REPEATED_MAX_MIN_MAX_DISTANCE, _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE);


                    DoLog($"Trendlines for indicator {indicator.Security.Symbol} successfully initalizing ", Constants.MessageType.Information);
                }

                DoLog($"Portfolio Position for indicator {indicator.Security.Symbol} successfully initialized",
                    Constants.MessageType.Information);
            }
        }

        protected override void InitializeManagers(string connStr)
        {

            TurtlesPortfolioPositionManager = new TurtlesPortfolioPositionManager(connStr);

            base.InitializeManagers(connStr);

        }



        protected void EvalHistoricalPricesPrecalculations(MonitoringPosition monPos)
        {
            if (monPos.IsTrendlineMonPosition())
            {
                DoLog($"Buiding trendlines for indicator {monPos.Security.Symbol}", Constants.MessageType.Information);
                BuildTrendlines((MonTrendlineTurtlesPosition)monPos,GetTrendlineIndicator(monPos).GetInnerTrendlinesSpan());
            }
        }

        protected ITrendlineIndicator GetTrendlineIndicator(MonitoringPosition indicator)
        {
            try
            {

                return (ITrendlineIndicator)indicator;

            }
            catch (Exception ex) {

                throw new Exception($"CRITICAL error: indicator for security {indicator.Security.Symbol} does NOT implement interface ITrendlineIndicator: Indicator type is {indicator.GetType()}");
            
            }
        
        
        }

        protected ITradingEnity GetTradingEntity(object entity)
        {
            try
            {
                return (ITradingEnity)entity;
            }
            catch (Exception ex)
            {

                throw new Exception($"CRITICAL error: trading entity does NOT implement interface ITradingEnity: entity type is {entity.GetType()}");
            }
        }

        protected IMonPosition GetMonPositionConfigInterface(MonitoringPosition monPos)
        {
            try
            {

                return (IMonPosition)monPos;
            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error: monitoring position for security {monPos.Security.Symbol} does NOT implement interface IMonPosition: Monitoring Position type is {monPos.GetType()}");

            }
        }

        protected void EvalMarketDataCalculations(MonitoringPosition indicator, MarketData md)
        {
            DoLog($"Recv markt data for indicator {indicator.Security.Symbol}: LastTrade={md.Trade} @{md.GetReferenceDateTime()}",Constants.MessageType.Information);
            bool newCandle = indicator.AppendCandle(md);

            if (indicator.IsTrendlineMonPosition())
            {
                if (newCandle)
                {
                    if (indicator.EvalSignalTriggered())
                    {
                        DoLog($"SIGNAL TRIGGERED!-->{indicator.SignalTriggered()}", Constants.MessageType.Information);
                    }
                    DoLog($"DBG1-Recalculating new trendlines at {DateTime.Now} for symbol {indicator.Security.Symbol} ---> Do Recalculate = {GetTrendlineIndicator(indicator).GetRecalculateTrendlines()}", Constants.MessageType.Information);

                    ITrendlineIndicator trndInd = GetTrendlineIndicator(indicator);
                    EvalBrokenTrendlines((MonTrendlineTurtlesPosition)indicator, md,trndInd.GetSkipCandlesToBreakTrndln());
                    RecalculateNewTrendlines((MonTrendlineTurtlesPosition)indicator, trndInd.GetRecalculateTrendlines());
                }

            }
            else
            {
                if (newCandle)
                {
                    if (indicator.EvalSignalTriggered())
                    {
                        DoLog($"SIGNAL TRIGGERED!-->{indicator.SignalTriggered()}", Constants.MessageType.Information);

                    }
                }
            }
        }

        private Security BuildFromConfigSecurityToMonitor(SecurityToMonitor secToMon)
        {
            Security sec = new Security()
            {
                Symbol = secToMon.Symbol,
                Currency = secToMon.Currency,
                Exchange = secToMon.Exchange,
                SecType = Security.GetSecurityType(secToMon.SecurityType),
                MarketData = new MarketData() { SettlType = SettlType.Tplus2 },


            };
            return sec;

        }

        protected void EvalSendBulkMarketDataRequest()
        {
            int securitiesToReq = (GetConfig().ChainedTurtleIndicators != null ? GetConfig().ChainedTurtleIndicators.Count : 0)
                                 + (GetConfig().SecuritiesToMonitor != null ? GetConfig().SecuritiesToMonitor.Count : 0);


            if (ProcessedHistoricalPrices.Count == securitiesToReq)
            {
                //All the historical pricess have been received


                List<Security> securities = new List<Security>();
               //1- Build all the SecuritiesToMonitor

                foreach (var secToMon in GetConfig().SecuritiesToMonitor)
                {
                    securities.Add(BuildFromConfigSecurityToMonitor(secToMon));
                }


                foreach (var indicator in GetConfig().ChainedTurtleIndicators)
                {
                    securities.Add(BuildFromConfigSecurityToMonitor(indicator.SecurityToMonitor));
                }


                MarketDataRequestBulkWrapper reqBulkWr = new MarketDataRequestBulkWrapper(securities, SubscriptionRequestType.SnapshotAndUpdates);
                MarketDataRequestCounter++;
                OnMessageRcv(reqBulkWr);
            }
        }

        protected void DoRequestMarketData(Security sec)
        {
            DoLog($"Requesting market data for security/indicator {sec.Symbol}", Constants.MessageType.Information);
            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
            MarketDataRequestCounter++;
            OnMessageRcv(wrapper);

        }

        protected void DoRequestMarketData(MonTurtlePosition monPos,string symbolRecv=null)
        {
            if (!GetConfig().BulkMarketDataRequest)
            {

                if (symbolRecv == null)
                {

                    foreach (Security sec in monPos.GetSecurities())
                    {
                        DoRequestMarketData(sec);
                    }
                }
                else
                {
                    Security sec = monPos.GetSecurities().Where(x=>x.Symbol==symbolRecv).FirstOrDefault();

                    if (sec != null)
                        DoRequestMarketData(sec);
                    else
                        throw new Exception($"Could not find indicator security {symbolRecv} to track!");
                }
            }
            else
            {
                // Only will be triggered on the last historical price received
                EvalSendBulkMarketDataRequest();


            }
        }

        protected void LogHistoricalPricesReceived(HistoricalPricesDTO dto)
        {
            MarketData oldestPrice = null;
            MarketData newestPrice = null;

            if (dto.MarketData != null)
            {
                oldestPrice = dto.MarketData.OrderBy(x => x.GetReferenceDateTime()).FirstOrDefault();
                newestPrice = dto.MarketData.OrderByDescending(x => x.GetReferenceDateTime()).FirstOrDefault();
            }

            string strOldestPr = oldestPrice != null && oldestPrice.Trade.HasValue ? oldestPrice.Trade.Value.ToString("#.##") : "?";
            string strNewestPr = newestPrice != null && newestPrice.Trade.HasValue ? newestPrice.Trade.Value.ToString("#.##") : "?";
            string strOldestHr = oldestPrice != null && oldestPrice.GetReferenceDateTime()!=null ? oldestPrice.GetReferenceDateTime().Value.ToString("MM/dd/yyyy HH:mm:ss") : "?";
            string strNewstHr = newestPrice != null && newestPrice.GetReferenceDateTime() != null ? newestPrice.GetReferenceDateTime().Value.ToString("MM/dd/yyyy HH:mm:ss") : "?";


            DoLog($"@{Config.Name}--> Recv Historical Prices for Symbol {dto.Symbol}: From={strOldestHr} FromVal={strNewstHr} To={strNewestPr} ToVal={strNewestPr}",Constants.MessageType.Information);

        }

        protected override void ProcessHistoricalPrices(object pWrapper)
        {
            try
            {
                HistoricalPricesDTO dto = null;

                dto = LoadHistoricalPrices((HistoricalPricesWrapper)pWrapper);
                LogHistoricalPricesReceived(dto);


                if (dto != null && dto.Symbol != null)
                {
                    lock (tSynchronizationLock)
                    {
                        bool foundSecurity = false;
                        if (MonitorPositions.ContainsKey(dto.Symbol))
                        {
                            MonTurtlePosition monPos = (MonTurtlePosition)MonitorPositions[dto.Symbol];
                            dto.MarketData.ForEach(x => monPos.AppendCandleHistorical(x));
                            ProcessedHistoricalPrices.Add(dto.Symbol);
                            DoRequestMarketData(monPos);
                            foundSecurity=true;
                            //If I am going to calculate the trendlines , it should be setup as an INDICATOR!!
                        }
                        
                        if(ChainedIndicators.Values.Any(x=>  x.GetSecurities().Any(w=>w.Symbol==dto.Symbol   )))
                        {

                            foundSecurity = true;
                            foreach (var indicator in ChainedIndicators.Values.Where(x=> x.GetSecurities().Any(w=>w.Symbol==dto.Symbol)  ))
                            {
                                dto.MarketData.ForEach(x => indicator.AppendCandleHistorical(x));

                                EvalHistoricalPricesPrecalculations(indicator);//already updates the ProcessedHistoricalPrices
                                DoRequestMarketData(indicator,dto.Symbol);
                            }

                        }
                        
                        if(!foundSecurity)
                            DoLog($"Could not find monitoring/indicator position for symbol {dto.Symbol} processing historical prices", Constants.MessageType.Debug);

                    }
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("Critical ERROR processing Historical Prices! : {0}", e.Message),
                    Constants.MessageType.Error);
            }


        }

        protected override void DoPersistPosition(PortfolioPosition trdPos)
        {
            if (MonitorPositions.ContainsKey(trdPos.CurrentPos().Security.Symbol))
            {
                lock (tPersistLock)
                {
                    PortfTurtlesPosition portfPos = (PortfTurtlesPosition)trdPos;
                    TurtlesPortfolioPositionManager.Persist(portfPos);
                }

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

                HistoricalPricesRequetsID = 1;

                ChainedIndicators = new Dictionary<string, MonTurtlePosition>();
                ProcessedHistoricalPrices = new List<string>();

                LoadCustomTurtlesWindows();

                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

                //InitializeIndicators(pOnLogMsg);

                InitializeManagers(GetConfig().ConnectionString);

                Thread depuarateThread = new Thread(EvalDepuratingPositionsThread);
                depuarateThread.Start();

                Thread persistTrendlinesThread = new Thread(new ParameterizedThreadStart(DoPersistTrendlinesThread));
                persistTrendlinesThread.Start();

                if (!GetConfig().SkipTrendlinesDeletion.HasValue || !GetConfig().SkipTrendlinesDeletion.Value)
                {

                    Thread delPrevTrehdlinesThread = new Thread(new ParameterizedThreadStart(DeleteAllTrendlines));
                    delPrevTrehdlinesThread.Start();
                }
                else
                {
                    DoLog($"Skipping deleting trendlines because of config SkipTrendlinesDeletion={GetConfig().SkipTrendlinesDeletion}", Constants.MessageType.PriorityInformation);
                
                }

                //DoRefreshTrendlines --> In case we want to implement some manual implementation of the trendlines

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.BOBDayTurtles.BusinessEntities;
using tph.BOBDayTurtles.Common.Configuration;
using tph.BOBDayTurtles.Common.Util;
using tph.BOBDayTurtles.DataAccessLayer;
using tph.BOBDayTurtles.LogicLayer.Util;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.DataAccessLayer;
using zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;

namespace tph.BOBDayTurtles.LogicLayer
{
    public class BOBDayTurtles : DayTurtles.LogicLayer.DayTurtles
    {

        #region Protected Attributes

        protected List<string> ProcessedHistoricalPrices { get; set; }

        protected BOBTurtlesPortfolioPositionManager BOBTurtlesPortfolioPositionManager { get; set; }

        protected TrendlineManager TrendlineManager { get; set; }

        #endregion

        #region Overriden Methods

        public override void DoLoadConfig(string configFile, List<string> noValFlds)
        {
            if (Config == null)
                Config = ConfigLoader.GetConfiguration<Configuration>(this, configFile, noValFlds);
        }

        public Configuration GetConfig()
        {
            return (Configuration) Config;
        }

        #endregion

        #region Private Methods

        private void BuildTrendlines(string symbol)
        {
            MonBOBTurtlePosition portfPos = (MonBOBTurtlePosition) PortfolioPositionsToMonitor[symbol];
            List<MarketData> histPrices = new List<MarketData>(portfPos.Candles.Values);
            histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

            DoLog(string.Format("Received historical candles for symbol {0}:{1} candles", symbol, histPrices.Count),
                Constants.MessageType.Information);

            List<Trendline> resistances = TrendLineCreator.BuildResistances(portfPos.Security, histPrices, GetConfig());
            List<Trendline> supports = TrendLineCreator.BuildSupports(portfPos.Security, histPrices, GetConfig());
            portfPos.PopulateTrendlines(resistances, supports);

            List<Trendline> activeResistancces = resistances.Where(x => x.BrokenDate == null).ToList();
            foreach (Trendline resistance in activeResistancces)
            {
                DoLog(
                    string.Format("Found prev resistance for symbol {2} --> Start={0} End={1}", resistance.StartDate,
                        resistance.EndDate, resistance.Symbol), Constants.MessageType.Information);
            }

            List<Trendline> activeSupports = supports.Where(x => x.BrokenDate == null).ToList();
            foreach (Trendline support in activeSupports)
            {
                DoLog(
                    string.Format("Found prev support for symbol {2} --> Start={0} End={1}", support.StartDate,
                        support.EndDate, support.Symbol), Constants.MessageType.Information);
            }

            TrendLineCreator.ResetJustFound(portfPos.Security);

            DoLog(string.Format("Trendlines calculated for symbol {0}", symbol), Constants.MessageType.Information);

            ProcessedHistoricalPrices.Add(symbol);
        }

        protected void RecalculateNewTrendlines(MonBOBTurtlePosition portfPos)
        {
            try
            {
                List<MarketData> histPrices = new List<MarketData>(portfPos.Candles.Values);
                histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

                List<Trendline> newResistances =
                    TrendLineCreator.UpdateResistances(portfPos.Security, histPrices, portfPos.GetLastCandle());

                List<Trendline> newSupports =
                    TrendLineCreator.UpdateSupports(portfPos.Security, histPrices, portfPos.GetLastCandle());

                foreach (Trendline newRes in newResistances)
                {
                    DoLog(String.Format("Found new resistance for symbol {0}: StartDate={1} EndDate={2} Broken={3}",
                            newRes.Security.Symbol, newRes.StartDate, newRes.EndDate, newRes.GetBrokenData()),
                        Constants.MessageType.Information);
                    portfPos.AppendResistance(newRes);

                }

                foreach (Trendline newSupport in newSupports)
                {
                    DoLog(String.Format("Found new support for symbol {0}: StartDate={1} EndDate={2} Broken={3}",
                            newSupport.Security.Symbol, newSupport.StartDate, newSupport.EndDate,
                            newSupport.GetBrokenData()),
                        Constants.MessageType.Information);
                    portfPos.AppendSupport(newSupport);

                }

                TrendLineCreator.ResetJustFound(portfPos.Security);
            }
            catch (Exception e)
            {
                DoLog(
                    string.Format("Critical ERROR recalculating new trendlines for symbol {0}:{1}",
                        portfPos.Security.Symbol, e.Message), Constants.MessageType.Error);
            }


        }

        #endregion

        #region Protected Methods

        protected void EvalOpeningClosingPositions(MonTurtlePosition turtlePos)
        {

            TimeSpan elapsed = DateTime.Now - StartTime;

            if (TradingPositions.Keys.Count < Config.MaxOpenedPositions
                && !TradingPositions.ContainsKey(turtlePos.Security.Symbol)
                && IsTradingTime()
            )
            {
                EvalOpeningPosition(turtlePos);
            }
            else if (TradingPositions.ContainsKey(turtlePos.Security.Symbol) && IsTradingTime())
            {
                EvalClosingPosition(turtlePos);
                EvalClosingPositionOnStopLossHit(turtlePos);
                EvalAbortingOpeningPositions(turtlePos);
                EvalAbortingClosingPositions(turtlePos);
            }
        }

        protected override async void ProcessHistoricalPrices(object pWrapper)
        {

            try
            {
                lock (tLock)
                {
                    HistoricalPricesWrapper historicalPricesWrapper = (HistoricalPricesWrapper) pWrapper;

                    List<Wrapper> mdWrappers = (List<Wrapper>) historicalPricesWrapper.GetField(Fields.NULL);

                    string symbol = null;
                    foreach (MarketDataWrapper mdWrp in mdWrappers)
                    {
                        MarketData md = MarketDataConverter.GetMarketData(mdWrp, GetConfig());

                        if (PortfolioPositionsToMonitor.ContainsKey(md.Security.Symbol) && Securities != null)
                        {
                            MonBOBTurtlePosition portfPos =
                                (MonBOBTurtlePosition) PortfolioPositionsToMonitor[md.Security.Symbol];
                            portfPos.AppendCandle(md);
                            symbol = md.Security.Symbol;
                        }
                    }

                    if (symbol != null)
                    {
                        BuildTrendlines(symbol);
                    }
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("Critical ERROR processing Trendlines : {0}", e.Message),
                    Constants.MessageType.Error);
            }
        }

        protected override async void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper) pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            try
            {
                lock (tLock)
                {
                    if (PortfolioPositionsToMonitor.ContainsKey(md.Security.Symbol) && Securities != null
                                                                                    && ProcessedHistoricalPrices
                                                                                        .Contains(md.Security.Symbol))
                    {
                        MonBOBTurtlePosition portfPos =
                            (MonBOBTurtlePosition) PortfolioPositionsToMonitor[md.Security.Symbol];
                        if (portfPos.HasHistoricalCandles())
                        {
                            bool newCandle = portfPos.AppendCandle(md);

                            EvalOpeningClosingPositions(portfPos);
                            UpdateLastPrice(portfPos, md);

                            if (newCandle) //THIS MUST BE EVALUATED AFTER THE EvalOpening
                                RecalculateNewTrendlines(portfPos);
                        }
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

        protected override TradingPosition DoOpenTradingRegularPos(Position pos, PortfolioPosition portfPos)
        {
            MonBOBTurtlePosition bobPos = (MonBOBTurtlePosition) portfPos;
            return new TradBOBTurtlesPosition()
            {
                StrategyName = Config.Name,
                OpeningDate = DateTime.Now,
                OpeningPosition = pos,
                OpeningPortfolioPosition = portfPos,
                FeeTypePerTrade = Config.FeeTypePerTrade,
                FeeValuePerTrade = Config.FeeValuePerTrade,
                OpeningTrendline = bobPos.LastOpenTrendline
            };
        }

        protected override void DoPersist(TradingPosition trdPos)
        {
            if (PortfolioPositionsToMonitor.ContainsKey(trdPos.CurrentPos().Security.Symbol))
            {
                lock (tPersistLock)
                {
                    TradBOBTurtlesPosition turtlesTradPos = (TradBOBTurtlesPosition) trdPos;
                    BOBTurtlesPortfolioPositionManager.PersistPortfolioPositionTrade(turtlesTradPos);
                }

            }
        }

        protected override void LoadMonitorsAndRequestMarketData()
        {
            Thread.Sleep(5000);
            foreach (string symbol in Config.StocksToMonitor)
            {
                if (!PortfolioPositionsToMonitor.ContainsKey(symbol))
                {
                    Security sec = new Security()
                    {
                        Symbol = symbol,
                        SecType = Security.GetSecurityType(Config.SecurityTypes),
                        MarketData = new MarketData() {SettlType = SettlType.Tplus2},
                        Currency = Config.Currency,
                        Exchange = Config.Exchange
                    };

                    MonBOBTurtlePosition portfPos = new MonBOBTurtlePosition(GetConfig().OpenWindow,
                        GetConfig().CloseWindow,
                        GetConfig().StopLossForOpenPositionPct,
                        GetConfig().OuterTrendlineSpan)
                    {
                        Security = sec,
                        DecimalRounding = Config.DecimalRounding,
                    };

                    //1- We add the current security to monitor
                    PortfolioPositionsToMonitor.Add(symbol, portfPos);

                    Securities.Add(sec); //So far, this is all wehave regarding the Securities

                    //2- We request market data

                    MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec,
                        SubscriptionRequestType.SnapshotAndUpdates);
                    MarketDataRequestCounter++;
                    OnMessageRcv(wrapper);
                }
            }
        }

        private void DoPersist(MonBOBTurtlePosition portfPos, List<Trendline> trendlines)
        {
            foreach (Trendline trendline in trendlines)
            {
                if (!trendline.BrokenDate.HasValue)
                    TrendlineManager.Persist(trendline, portfPos);
                else
                {
                    if (!trendline.Persisted)
                    {
                        TrendlineManager.Persist(trendline, portfPos);
                        trendline.Persisted = true;
                    }
                }
            }
        }

        private void DoUpdate(Trendline updTrendline,Trendline memTrendline,MonBOBTurtlePosition monPos)
        {
            if (memTrendline != null)
            {
                memTrendline.BrokenDate = DateTime.Now;
                memTrendline.Modified = true;
                memTrendline.BrokenTrendlinePrice = 0;

            }
            else
                throw new Exception($"Error locating trendline Id {updTrendline.Id} --> not found in memory!");
                                
            updTrendline.BrokenDate=DateTime.Now;
            updTrendline.BrokenTrendlinePrice = 0;
            updTrendline.ToDisabled = false;
            updTrendline.Disabled = true;
            TrendlineManager.Persist(updTrendline,monPos);
        }

        private Trendline UpdateResistance(Trendline updTrendline,MonBOBTurtlePosition monPos)
        {
            Trendline memTrendline = monPos.Resistances
                .FirstOrDefault(x => DateTime.Compare(x.StartDate, updTrendline.StartDate) == 0
                                     && DateTime.Compare(x.EndDate, updTrendline.EndDate) == 0
                                     && x.TrendlineType == updTrendline.TrendlineType);


            DoUpdate(updTrendline, memTrendline, monPos);

            return memTrendline;
        }
        
        private Trendline UpdateSupport(Trendline updTrendline,MonBOBTurtlePosition monPos)
        {
            Trendline memTrendline = monPos.Supports
                .FirstOrDefault(x => DateTime.Compare(x.StartDate, updTrendline.StartDate) == 0
                                     && DateTime.Compare(x.EndDate, updTrendline.EndDate) == 0
                                     && x.TrendlineType == updTrendline.TrendlineType);


            DoUpdate(updTrendline, memTrendline, monPos);

            return memTrendline;
        }

        protected void DoRefreshTrendlines(object param)
        {
            while (true)
            {
                try
                {
                    List<Trendline> toRefresh =  TrendlineManager.GetTrendlines();
                    lock (tLock)
                    {
                        foreach (Trendline updTrendline in toRefresh)
                        {
                            MonBOBTurtlePosition monPos =(MonBOBTurtlePosition) PortfolioPositionsToMonitor[updTrendline.Security.Symbol];


                            if (updTrendline.ToDisabled.HasValue && updTrendline.ToDisabled.Value)
                            {
                                if (updTrendline.TrendlineType == TrendlineType.Resistance)
                                    UpdateResistance(updTrendline, monPos);
                                
                                if (updTrendline.TrendlineType == TrendlineType.Support)
                                    UpdateSupport(updTrendline, monPos);
                            }


                            if (updTrendline.ManualNew.HasValue && updTrendline.ManualNew.Value)
                            {
                                if (updTrendline.TrendlineType==TrendlineType.Resistance)
                                {
                                    monPos.Resistances.Add(updTrendline);
                                }
                                else if(updTrendline.TrendlineType==TrendlineType.Support)
                                {
                                    monPos.Supports.Add(updTrendline);
                                }

                                updTrendline.ManualNew = false;
                                TrendlineManager.Persist(updTrendline,monPos);

                            }

                        }
                    }

                    Thread.Sleep(1000);//1 sec sleep
                }
                catch (Exception e)
                {
                    DoLog(string.Format("@BOBDayTurtles - Critical ERROR @DoRefreshTrendlines:{0}",e.Message),Constants.MessageType.Error);
                }
            }
            
        }

        protected void DoPersistTrendlinesThread(object param)
        {
            while (true)
            {
                try
                {
                    lock (tLock)
                    {

                        foreach (MonBOBTurtlePosition portfPos in PortfolioPositionsToMonitor.Values)
                        {
                            DoPersist(portfPos,portfPos.Resistances);
                            DoPersist(portfPos,portfPos.Supports);
                        }
                        
                    }

                    Thread.Sleep(5000);//5 seconds sleep
                }
                catch (Exception e)
                {
                    DoLog(string.Format("@BOBDayTurtles - Critical ERROR Persting Trendlines:{0}",e.Message),Constants.MessageType.Error);
                }
            }
            
        }

        protected void DoRequestHistoricalPricesThread(object param)
        {
            try
            {
                int i = 1;
                foreach (string symbol in PortfolioPositionsToMonitor.Keys)
                {
                    HistoricalPricesRequestWrapper reqWrapper = new HistoricalPricesRequestWrapper(i,symbol,DateTime.Now.AddMinutes(-1000),null,CandleInterval.Minute_1);
                    OnMessageRcv(reqWrapper);
                    i++;
                }
            }
            catch (Exception e)
            {
               DoLog(string.Format("@BOBDayTurtles - Critical ERROR Requesting Historical Prices:{0}",e.Message),Constants.MessageType.Error);
            }
        }
        
        protected override void InitializeManagers()
        {
            BOBTurtlesPortfolioPositionManager= new BOBTurtlesPortfolioPositionManager(GetConfig().ConnectionString);
        }
        
        #endregion
        
        #region Public Methods
        
        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                ProcessedHistoricalPrices=new List<string>();

                Trendline._SHORT_SOFT_UPWARD_SLOPE = GetConfig().MaxShortPositiveSlope;
                Trendline._SHORT_SOFT_DOWNARD_SLOPE = GetConfig().MaxShortNegativeSlope;
                Trendline._LONG_SOFT_UPWARD_SLOPE = GetConfig().MaxLongPositiveSlope;
                Trendline._LONG_SOFT_DOWNARD_SLOPE = GetConfig().MaxLongNegativeSlope;
                
                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);
                
                TrendlineManager= new TrendlineManager(GetConfig().ConnectionString);

                Thread.Sleep(2000);
                
                Thread historicalPricesThread = new Thread(new ParameterizedThreadStart(DoRequestHistoricalPricesThread));
                historicalPricesThread.Start();
                
                Thread persistTrendlinesThread = new Thread(new ParameterizedThreadStart(DoPersistTrendlinesThread));
                persistTrendlinesThread.Start();
                
                Thread refreshTrendlinesThread = new Thread(new ParameterizedThreadStart(DoRefreshTrendlines));
                refreshTrendlinesThread.Start();
                
                //
                
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
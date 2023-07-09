using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.DataAccessLayer;
using tph.TrendlineTurtles.LogicLayer.Util;
using zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Wrappers;

namespace tph.TrendlineTurtles.LogicLayer
{
    public class TrendlineTurtles:DayTurtles.LogicLayer.DayTurtles
    {
        #region Public Attributes
        
        protected TrendlineManager TrendlineManager { get; set; }
        
        protected List<string> ProcessedHistoricalPrices { get; set; }
        
        protected TrendlineTurtlesPortfolioPositionManager TrendlineTurtlesPortfolioPositionManager { get; set; }
        
        #endregion
     
        #region Protected Methods
        
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
                        MonTrendlineTurtlesPosition portfPos =(MonTrendlineTurtlesPosition) PortfolioPositionsToMonitor[md.Security.Symbol];
                        if (portfPos.HasHistoricalCandles())
                        {
                            bool newCandle = portfPos.AppendCandle(md);

                            EvalOpeningClosingPositions(portfPos);
                            UpdateLastPrice(portfPos, md);

                            if (newCandle && GetConfig().RecalculateTrendlines) //THIS MUST BE EVALUATED AFTER THE EvalOpening
                                RecalculateNewTrendlines(portfPos);
                            else
                                EvalBrokenTrendlines(portfPos, md);
                            
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
                            MonTrendlineTurtlesPosition portfPos =
                                (MonTrendlineTurtlesPosition) PortfolioPositionsToMonitor[md.Security.Symbol];
                            portfPos.AppendCandle(md);
                            symbol = md.Security.Symbol;
                        }
                    }

                    if (symbol != null )
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
        
        protected void BuildTrendlines(string symbol)
        {
            MonTrendlineTurtlesPosition portfPos = (MonTrendlineTurtlesPosition) PortfolioPositionsToMonitor[symbol];
            
            List<MarketData> histPrices = new List<MarketData>(portfPos.Candles.Values);
            histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

            if (GetConfig().RecalculateTrendlines)
            {

                DoLog(string.Format("Received historical candles for symbol {0}:{1} candles", symbol, histPrices.Count),
                    Constants.MessageType.Information);

                List<Trendline> resistances =
                    TrendLineCreator.BuildResistances(portfPos.Security, histPrices, GetConfig());
                List<Trendline> supports = TrendLineCreator.BuildSupports(portfPos.Security, histPrices, GetConfig());
                portfPos.PopulateTrendlines(resistances, supports);

                List<Trendline> activeResistancces = resistances.Where(x => x.BrokenDate == null).ToList();
                foreach (Trendline resistance in activeResistancces)
                {
                    DoLog(
                        string.Format("Found prev resistance for symbol {2} --> Start={0} End={1}",
                            resistance.StartDate,
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
            }

            ProcessedHistoricalPrices.Add(symbol);
       
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            bool init= base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

            if (init)
            {
                foreach (var monPosition in PortfolioPositionsToMonitor.Values)
                {
                    TrendLineCreator.InitializeCreator(monPosition.Security, GetConfig(),
                        DateTime.Now.AddMinutes(-1000));
                    DoLog($"Portfolio Position for symbol {monPosition.Security.Symbol} successfully initialized",
                        Constants.MessageType.Information);
                }
            }

            return init;
        }

        protected void EvalBrokenTrendlines(MonTrendlineTurtlesPosition portfPos,MarketData price)
        {
            TrendLineCreator.EvalBrokenTrendlines(portfPos,price);
            
        }

        protected void RecalculateNewTrendlines(MonTrendlineTurtlesPosition portfPos)
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
        
        protected virtual void DoPersistPosition(TradingPosition trdPos)
        {
            if (PortfolioPositionsToMonitor.ContainsKey(trdPos.CurrentPos().Security.Symbol))
            {
                lock (tPersistLock)
                {
                    TradTrendlineTurtlesPosition turtlesTradPos = (TradTrendlineTurtlesPosition) trdPos;
                    TrendlineTurtlesPortfolioPositionManager.PersistPortfolioPositionTrade(turtlesTradPos);
                }

            }
        }
        
        private void DoPersistTrendline(MonTrendlineTurtlesPosition portfPos, List<Trendline> trendlines)
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
        
        private void DoUpdate(Trendline updTrendline,Trendline memTrendline,MonTurtlePosition monPos,bool persit=false)
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
            
            if(persit)
                TrendlineManager.Persist(updTrendline,monPos);
        }

        private Trendline UpdateResistance(Trendline updTrendline,MonTrendlineTurtlesPosition monPos)
        {
            Trendline memPosTrendine = monPos.Resistances
                .FirstOrDefault(x => DateTime.Compare(x.StartDate, updTrendline.StartDate) == 0
                                     && DateTime.Compare(x.EndDate, updTrendline.EndDate) == 0
                                     && x.TrendlineType == updTrendline.TrendlineType);

            Trendline memCrtrTrendline = TrendLineCreator.FetchResistance(monPos.Security.Symbol, updTrendline);


            DoUpdate(updTrendline, memPosTrendine, monPos,true);
            DoUpdate(updTrendline, memCrtrTrendline, monPos);

            return memPosTrendine;
        }
        
        private Trendline UpdateSupport(Trendline updTrendline,MonTrendlineTurtlesPosition monPos)
        {
            Trendline memPosTrendline = monPos.Supports
                .FirstOrDefault(x => DateTime.Compare(x.StartDate, updTrendline.StartDate) == 0
                                     && DateTime.Compare(x.EndDate, updTrendline.EndDate) == 0
                                     && x.TrendlineType == updTrendline.TrendlineType);
            
            
            Trendline memCrtrTrendline = TrendLineCreator.FetchSupport(monPos.Security.Symbol, updTrendline);


            DoUpdate(updTrendline, memPosTrendline, monPos,true);
            DoUpdate(updTrendline, memCrtrTrendline, monPos);

            return memPosTrendline;
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
                            if (!TrendLineCreator.SymbolInitialized(updTrendline.Symbol))
                            {
                                DoLog($"WARNING - Waiting for symbo. {updTrendline.Symbol} to be initialized to process trendlines",Constants.MessageType.Information);
                                continue;
                            }
                            

                            MonTrendlineTurtlesPosition monPos =(MonTrendlineTurtlesPosition) PortfolioPositionsToMonitor[updTrendline.Security.Symbol];


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
                                    monPos.AppendResistance(updTrendline);
                                    TrendLineCreator.AppendResistance(updTrendline.Security.Symbol,updTrendline);
                                    
                                }
                                else if(updTrendline.TrendlineType==TrendlineType.Support)
                                {
                                    monPos.AppendSupport(updTrendline);
                                    TrendLineCreator.AppendSupport(updTrendline.Security.Symbol,updTrendline);
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

                        foreach (MonTrendlineTurtlesPosition portfPos in PortfolioPositionsToMonitor.Values)
                        {
                            DoPersistTrendline(portfPos,portfPos.Resistances);
                            DoPersistTrendline(portfPos,portfPos.Supports);
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

        protected virtual void InitializeManagers(string connStr)
        {
            TrendlineTurtlesPortfolioPositionManager= new TrendlineTurtlesPortfolioPositionManager(connStr);
            
            TrendlineManager= new TrendlineManager(connStr);
        }

        
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.Common.Configuration;
using tph.TrendlineTurtles.DataAccessLayer;
using tph.TrendlineTurtles.LogicLayer.Util;
using zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.DTO;
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

        #region Protected Static Consts

        protected static int _REPEATED_MAX_MIN_MAX_DISTANCE = 5;

        protected static int _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE = 5;

        #endregion

        #region Protected Static Consts

        public static int _DAYS_TO_SUBSTRACT = -10;

        #endregion

        #region Protected Methods

        public virtual TrendlineConfiguration GetConfig()
        {
            return (TrendlineConfiguration)Config;
        }

        protected override void DoRequestHistoricalPricesThread(object param)
        {
            try
            {
                int i = 1;

                foreach (string symbol in MonitorPositions.Keys)
                {
                    DateTime from = DateTimeManager.Now.AddDays(GetConfig().HistoricalPricesPeriod);
                    //DateTime to = DateTimeManager.Now.AddDays(1);
                    DateTime to = DateTimeManager.Now;
                    
                    HistoricalPricesRequestWrapper reqWrapper = new HistoricalPricesRequestWrapper(i, symbol, from, to, CandleInterval.Minute_1,
                                                                                                    Config.Currency, SecurityTypeTranslator.TranslateMandatorySecurityType(Config.SecurityTypes), Config.Exchange);
                    OnMessageRcv(reqWrapper);
                    i++;
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("@BOBDayTurtles - Critical ERROR Requesting Historical Prices:{0}", e.Message), Constants.MessageType.Error);
            }
        }

        protected override async void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper) pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);
            
            OrderRouter.ProcessMessage(wrapper);

            try
            {
                lock (tLock)
                {
                    if (!ValidateMarketDataRec(md)) { return; }

                    DateTimeManager.NullNow = md.GetReferenceDateTime();
                    if (MonitorPositions.ContainsKey(md.Security.Symbol) && Securities != null
                                                                                    && ProcessedHistoricalPrices
                                                                                        .Contains(md.Security.Symbol))
                    {
                        MonTrendlineTurtlesPosition monPos =(MonTrendlineTurtlesPosition) MonitorPositions[md.Security.Symbol];
                        if (monPos.HasHistoricalCandles())
                        {
                            bool newCandle = monPos.AppendCandle(md);

                            EvalOpeningClosingPositions(monPos);
                            UpdateLastPrice(monPos, md);

                            //if (newCandle && GetConfig().RecalculateTrendlines) //THIS MUST BE EVALUATED AFTER THE EvalOpening
                            //    RecalculateNewTrendlines(monPos);
                            //else
                            //    EvalBrokenTrendlines(monPos, md);


                            //Always eval broken trendlines
                            //Not a problem w/ First candle of day scenario
                            // Becasue it works with the prev trendline
                            //10:30 --> uses 16:59 candle
                            //10:31 --> uses 10:30 candle and the position was already opened
                            if (newCandle)
                            {
                                RecalculateNewTrendlines(monPos, GetConfig().RecalculateTrendlines);
                                EvalBrokenTrendlines(monPos, md,GetConfig().SkipCandlesToBreakTrndln);
                            }
                        }
                    }
                }
                
            }
            catch (Exception e)
            {
                DoLog(
                    string.Format("ERROR @TrendlinesTurtlesLogicLayer- Error processing market data:{0}-{1}", e.Message, e.StackTrace),
                    Constants.MessageType.Error);
            }
        }
        
        protected override async void ProcessHistoricalPrices(object pWrapper)
        {
            try
            {
                HistoricalPricesDTO dto = null;
               
                dto=LoadHistoricalPrices((HistoricalPricesWrapper)pWrapper);
               

                if (dto != null && dto.Symbol!=null)
                {
                    lock (tSynchronizationLock)
                    {
                        if (MonitorPositions.ContainsKey(dto.Symbol))
                            BuildTrendlines((MonTrendlineTurtlesPosition)MonitorPositions[dto.Symbol],GetConfig().InnerTrendlinesSpan);
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
        
        protected void BuildTrendlines(MonTrendlineTurtlesPosition monPos,int innerTrendlinesSpan)
        {
            
            
            List<MarketData> histPrices = new List<MarketData>(monPos.Candles.Values);
            histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

            DoLog(string.Format("Received historical candles for symbol {0}:{1} candles", monPos.Security.Symbol, histPrices.Count),
                Constants.MessageType.Information);

            List<Trendline> resistances =
                TrendLineCreator.BuildResistances(monPos.Security, histPrices, innerTrendlinesSpan);
            List<Trendline> supports = TrendLineCreator.BuildSupports(monPos.Security, histPrices, innerTrendlinesSpan);
            monPos.PopulateTrendlines(resistances, supports);

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

            TrendLineCreator.ResetJustFound(monPos.Security);

            DoLog(string.Format("Trendlines calculated for symbol {0}", monPos.Security.Symbol), Constants.MessageType.Information);
            
            //Console.Beep();//DBG
            ProcessedHistoricalPrices.Add(monPos.Security.Symbol);
       
        }


        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            bool init= base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

            if (init)
            {
                foreach (var monPosition in MonitorPositions.Values)
                {
                    //if mo
                    if ( monPosition.IsTrendlineMonPosition())
                    {

                        DoLog($"Initializing Trendlines for Trendline Monitor Position for symbol {monPosition.Security.Symbol}",Constants.MessageType.Information);

                        TrendLineCreator.InitializeCreator(monPosition.Security,
                                                           GetConfig(),
                                                           DateTimeManager.Now.AddDays(GetConfig().HistoricalPricesPeriod),
                                                           pOnLogMsg);

                        DoLog($"Trendlines for Trendline Monitor Position for symbol {monPosition.Security.Symbol} successfully initialized", Constants.MessageType.Information);
                    }

                    DoLog($"Portfolio Position for symbol {monPosition.Security.Symbol} successfully initialized",
                        Constants.MessageType.Information);
                }
            }

            return init;
        }

        protected void EvalBrokenTrendlines(MonTrendlineTurtlesPosition monPortfPos,MarketData price,int skipCandlesToBreakTrndln)
        {
            DoLog($"DBG7- Evaluating broken trendlikes for symbol {monPortfPos.Security.Symbol} w/skipCandlesToBreakTrndln={skipCandlesToBreakTrndln}", Constants.MessageType.Information);
            MarketData priceToUse = monPortfPos.GetLastFinishedCandle(skipCandlesToBreakTrndln);

            TrendLineCreator.EvalBrokenTrendlines(monPortfPos, priceToUse);
            
        }

        protected void RecalculateNewTrendlines(MonTrendlineTurtlesPosition portfPos, bool doRecalculate)
        {
            try
            {
               
                List<MarketData> histPrices = new List<MarketData>(portfPos.Candles.Values);
                histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

                if (doRecalculate)
                {
                    DoLog($"DBG1-Trendline Creator Inner Status: {TrendLineCreator.LogInnerStatus(portfPos.Security.Symbol)}", Constants.MessageType.Debug);

                    List<Trendline> newResistances =
                        TrendLineCreator.UpdateResistances(portfPos.Security, histPrices, portfPos.GetLastFinishedCandle());

                    List<Trendline> newSupports =
                        TrendLineCreator.UpdateSupports(portfPos.Security, histPrices, portfPos.GetLastFinishedCandle());

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

                TrendLineCreator.MoveReferenceDateForResistances(histPrices, portfPos.Security.Symbol);
                TrendLineCreator.MoveReferenceDateForSupports(histPrices, portfPos.Security.Symbol);

            }
            catch (Exception e)
            {
                DoLog(
                    string.Format("Critical ERROR recalculating new trendlines for symbol {0}:{1}",
                        portfPos.Security.Symbol, e.Message), Constants.MessageType.Error);
            }


        }
        
        protected virtual void DoPersistPosition(PortfolioPosition trdPos)
        {
            if (MonitorPositions.ContainsKey(trdPos.CurrentPos().Security.Symbol))
            {
                lock (tPersistLock)
                {
                    PortfTrendlineTurtlesPosition turtlesTradPos = (PortfTrendlineTurtlesPosition) trdPos;
                    TrendlineTurtlesPortfolioPositionManager.Persist(turtlesTradPos);
                }

            }
        }

        protected override void DoPersist(PortfolioPosition trdPos)
        {
            DoPersistPosition(trdPos);
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
                memTrendline.BrokenDate = DateTimeManager.Now;
                memTrendline.Modified = true;
                memTrendline.BrokenTrendlinePrice = 0;

            }
            else
                throw new Exception($"Error locating trendline Id {updTrendline.Id} --> not found in memory!");
                                
            updTrendline.BrokenDate= DateTimeManager.Now;
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

        protected virtual void DeleteAllTrendlines(object param)
        {
            try
            {
                foreach (var monPos in MonitorPositions.Values)
                {
                    DoLog($"Deleting prev trendlines for symbol {monPos.Security.Symbol}", Constants.MessageType.Information);
                    TrendlineManager.Delete(monPos.Security.Symbol);
                    DoLog($"Prev trendlines for symbol {monPos.Security.Symbol} successfully deleted", Constants.MessageType.Information);

                    List<MonitoringPosition> innerIndicators = monPos.GetInnerIndicators();
                    if (innerIndicators != null)
                    {
                        foreach (var innerIndicator in innerIndicators)
                        {
                            if (innerIndicator is MonTrendlineTurtlesPosition)
                            {
                                DoLog($"Deleting prev trendlines for inner symbol {monPos.Security.Symbol}", Constants.MessageType.Information);
                                TrendlineManager.Delete(innerIndicator.Security.Symbol);
                                DoLog($"Prev trendlines for inner symbol {monPos.Security.Symbol} successfully deleted", Constants.MessageType.Information);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL ERROR deleting previousy existing trendlines: {ex.Message}", Constants.MessageType.Error);
            
            }
        
        }

        protected virtual void UpdateManualNew(Trendline updTrendline)
        {

            updTrendline.ManualNew = false;
        }

        protected void DoRefreshTrendlines(object param)
        {
            while (true)
            {
                try
                {
                    List<Trendline> toRefresh =  TrendlineManager.GetTrendlines();
                   
                    foreach (Trendline updTrendline in toRefresh)
                    {
                        DoLog($"Updating trendline {updTrendline.Id} for symbol {updTrendline.Security.Symbol}", Constants.MessageType.Information);
                        if (!TrendLineCreator.SymbolInitialized(updTrendline.Symbol))
                        {
                            DoLog($"WARNING - Waiting for symbol {updTrendline.Symbol} to be initialized to process trendlines",Constants.MessageType.Information);
                            continue;
                        }
                            

                        MonTrendlineTurtlesPosition monPos =(MonTrendlineTurtlesPosition) MonitorPositions[updTrendline.Security.Symbol];


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


                            UpdateManualNew(updTrendline);

                            if (monPos.HasHistoricalCandles())
                                TrendlineManager.Persist(updTrendline, monPos);
                            else
                                DoLog($"Skipping updating trendline as symbol {monPos.Security.Symbol} still does not have historical prices!", Constants.MessageType.Information);

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
                    //lock (tLock)
                    //{

                    foreach (MonTrendlineTurtlesPosition monPos in MonitorPositions.Values)
                    {
                        List<Trendline> resToPersist = new List<Trendline>(monPos.Resistances);
                        List<Trendline> supToPersist = new List<Trendline>(monPos.Supports);
                        DoPersistTrendline(monPos, resToPersist);
                        DoPersistTrendline(monPos, supToPersist);

                        List<MonitoringPosition> innerIndicators= monPos.GetInnerIndicators();

                        if (innerIndicators != null)
                        {
                            foreach(var innerIndicator in innerIndicators)
                            {
                                //MonTrendlineTurtlesPosition
                                if (innerIndicator is MonTrendlineTurtlesPosition)
                                {
                                    MonTrendlineTurtlesPosition trndIndicator = (MonTrendlineTurtlesPosition)innerIndicator;
                                    List<Trendline> innerResToPersist = new List<Trendline>(trndIndicator.Resistances);
                                    List<Trendline> innerSuppToPersist = new List<Trendline>(trndIndicator.Supports);
                                    DoPersistTrendline(trndIndicator, innerResToPersist);
                                    DoPersistTrendline(trndIndicator, innerSuppToPersist);
                                }
                            }
                        }
                    }
                        
                    //}

                    Thread.Sleep(2000);//2 seconds sleep
                }
                catch (Exception e)
                {
                    DoLog(string.Format("@BOBDayTurtles - Warning Persisting Trendlines:{0}",e.Message),Constants.MessageType.Debug);
                }
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
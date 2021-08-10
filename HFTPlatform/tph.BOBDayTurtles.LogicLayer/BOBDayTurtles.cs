using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.BOBDayTurtles.BusinessEntities;
using tph.BOBDayTurtles.Common.Configuration;
using tph.BOBDayTurtles.Common.Util;
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
    public class BOBDayTurtles:DayTurtles.LogicLayer.DayTurtles
    {
        
        #region Protected Attributes
          
        protected List<string> ProcessedHistoricalPrices { get; set; }
        
        protected TurtlesPortfolioPositionManager TurtlesPortfolioPositionManager { get; set; }
          
        #endregion
        
        #region Overriden Methods

        public override  void  DoLoadConfig(string configFile, List<string> noValFlds)
        {
            if(Config==null)
                Config = ConfigLoader.GetConfiguration<Configuration>(this, configFile, noValFlds);
        }

        public Configuration GetConfig()
        {
            return (Configuration) Config;
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

        private void BuildTrendlines(string symbol)
        {
                MonBOBTurtlePosition portfPos = (MonBOBTurtlePosition) PortfolioPositionsToMonitor[symbol];
                List<MarketData> histPrices = new List<MarketData>(portfPos.Candles.Values);
                histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();

    //                        foreach (MarketData md in histPrices.OrderBy(x => x.MDEntryDate))
    //                        {
    //                            DoLog(
    //                                string.Format("Processing historical price for symbol {0}: Date={1} Close={2}",
    //                                    md.Security.Symbol, md.MDEntryDate.Value, md.ClosingPrice),
    //                                Constants.MessageType.Information);
    //                        }


                DoLog(string.Format("Received historical candles for symbol {0}:{1} candles", symbol,histPrices.Count), Constants.MessageType.Information);

                List<Trendline> resistances = TrendLineCreator.BuildResistances(portfPos.Security, histPrices, GetConfig());
                List<Trendline> supports =TrendLineCreator.BuildSupports(portfPos.Security, histPrices, GetConfig());
                portfPos.PopulateTrendlines(resistances, supports);

                List<Trendline> activeResistancces = resistances.Where(x => x.BrokenDate == null).ToList();
                foreach (Trendline resistance in activeResistancces)
                {
                    DoLog(string.Format("Found active resistance for symbol {2} Start={0} End={1}",resistance.StartDate,resistance.EndDate,resistance.Symbol),Constants.MessageType.Information);                    
                }
                
                List<Trendline> activeSupports = supports.Where(x => x.BrokenDate == null).ToList();
                foreach (Trendline support in activeSupports)
                {
                    DoLog(string.Format("Found active support for symbol {2} Start={0} End={1}",support.StartDate,support.EndDate,support.Symbol),Constants.MessageType.Information);                    
                }
                
                
                DoLog(string.Format("Trendlines calculated for symbol {0}", symbol),Constants.MessageType.Information);
                            
                ProcessedHistoricalPrices.Add(symbol);
        }

        protected override void ProcessHistoricalPrices(object pWrapper)
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

        protected void RecalculateNewTrendlines(MonBOBTurtlePosition portfPos)
        {
            List<MarketData> histPrices = new List<MarketData>(portfPos.Candles.Values);
            histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();
            
            List<Trendline> newResistances = TrendLineCreator.UpdateResistances(portfPos.Security, histPrices, 
                                                                                GetConfig(), portfPos.Resistances, 
                                                                                portfPos.GetLastCandle());
            
            List<Trendline> newSupports = TrendLineCreator.UpdateSupports(portfPos.Security, histPrices, GetConfig(), portfPos.Supports, 
                                            portfPos.GetLastCandle());

            foreach (Trendline newRes in newResistances)
            {
                DoLog(String.Format("Found new resistance for symbol {0}: StartDate={1} EndDate={2}",
                                        newRes.Security.Symbol,newRes.StartDate,newRes.EndDate),Constants.MessageType.Information);
            }
            
            foreach (Trendline newRes in newSupports)
            {
                DoLog(String.Format("Found new support for symbol {0}: StartDate={1} EndDate={2}",
                    newRes.Security.Symbol,newRes.StartDate,newRes.EndDate),Constants.MessageType.Information);
            }
            
            portfPos.Resistances.AddRange(newResistances);
            portfPos.Supports.AddRange(newSupports);
            
            
        }

        protected override void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper) pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            try
            {
                lock (tLock)
                {
                    if (PortfolioPositionsToMonitor.ContainsKey(md.Security.Symbol) && Securities!=null
                        && ProcessedHistoricalPrices.Contains(md.Security.Symbol))
                    {
                        MonBOBTurtlePosition portfPos = (MonBOBTurtlePosition) PortfolioPositionsToMonitor[md.Security.Symbol];
                        if (portfPos.HasHistoricalCandles())
                        {
                            bool newCandle = portfPos.AppendCandle(md);

                            if (newCandle)
                                RecalculateNewTrendlines(portfPos);

                            EvalOpeningClosingPositions(portfPos);
                            UpdateLastPrice(portfPos, md);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("ERROR @DailyTurtles- Error processing market data:{0}-{1}",e.Message,e.StackTrace),Constants.MessageType.Error);
            }
        }

        protected override TradingPosition DoOpenTradingRegularPos(Position pos, PortfolioPosition portfPos)
        {
            return new TradBOBTurtlesPosition()
            {
                StrategyName = Config.Name,
                OpeningDate = DateTime.Now,
                OpeningPosition = pos,
                OpeningPortfolioPosition = portfPos,
                FeeTypePerTrade = Config.FeeTypePerTrade,
                FeeValuePerTrade = Config.FeeValuePerTrade
            };
        }

        protected override void DoPersist(TradingPosition trdPos)
        {
            if (PortfolioPositionsToMonitor.ContainsKey(trdPos.CurrentPos().Security.Symbol))
            {
                lock (tPersistLock)
                {
                    TradBOBTurtlesPosition turtlesTradPos = (TradBOBTurtlesPosition) trdPos;
                    TurtlesPortfolioPositionManager.PersistPortfolioPositionTrade(turtlesTradPos);
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
                        MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
                        Currency = Config.Currency,
                        Exchange = Config.Exchange
                    };

                    MonBOBTurtlePosition portfPos = new MonBOBTurtlePosition(GetConfig().OpenWindow,
                        GetConfig().CloseWindow,
                        GetConfig().StopLossForOpenPositionPct)
                    {
                        Security = sec,
                        DecimalRounding = Config.DecimalRounding,
                    };

                    //1- We add the current security to monitor
                    PortfolioPositionsToMonitor.Add(symbol, portfPos);

                    Securities.Add(sec);//So far, this is all wehave regarding the Securities

                    //2- We request market data

                    MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec, SubscriptionRequestType.SnapshotAndUpdates);
                    MarketDataRequestCounter++;
                    OnMessageRcv(wrapper);
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
            TurtlesPortfolioPositionManager= new TurtlesPortfolioPositionManager(GetConfig().ConnectionString);
        }
        
        #endregion
        
        #region Public Methods
        
        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;
            StartTime = DateTime.Now;
            LastCounterResetTime = StartTime;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                ProcessedHistoricalPrices=new List<string>();
                
                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

                Thread.Sleep(2000);
                
                Thread historicalPricesThread = new Thread(new ParameterizedThreadStart(DoRequestHistoricalPricesThread));
                historicalPricesThread.Start();
                
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.BOBDayTurtles.BusinessEntities;
using tph.BOBDayTurtles.Common.Configuration;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.DataAccessLayer;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.DataAccessLayer;
using tph.TrendlineTurtles.LogicLayer.Util;
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
    public class BOBDayTurtles : TrendlineTurtles.LogicLayer.TrendlineTurtles
    {

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

        #region Protected Methods

        protected void EvalOpeningClosingPositions(MonTurtlePosition turtlePos)
        {

            TimeSpan elapsed = DateTimeManager.Now - StartTime;

            if (PortfolioPositions.Keys.Count < Config.MaxOpenedPositions
                && !PortfolioPositions.ContainsKey(turtlePos.Security.Symbol)
                && IsTradingTime()
            )
            {
                lock (tSynchronizationLock)
                {
                    //Console.Beep(100, 1000);//DBG
                    EvalOpeningPosition(turtlePos);
                }
            }
            else if (PortfolioPositions.ContainsKey(turtlePos.Security.Symbol) && IsTradingTime())
            {
                EvalClosingPosition(turtlePos);
                EvalClosingPositionOnStopLossHit(turtlePos);
                EvalAbortingOpeningPositions(turtlePos);
                EvalAbortingClosingPositions(turtlePos);
            }
        }

        protected override TradingPosition DoOpenTradingRegularPos(Position routingPos, MonitoringPosition monPos)
        {
            MonBOBTurtlePosition bobMonPos = (MonBOBTurtlePosition) monPos;
            return new TradBOBTurtlesPosition()
            {
                StrategyName = Config.Name,
                OpeningDate = bobMonPos.GetCurrentCandleDate(),
                OpeningPosition = routingPos,
                OpeningPortfolioPosition = monPos,
                FeeTypePerTrade = Config.FeeTypePerTrade,
                FeeValuePerTrade = Config.FeeValuePerTrade,
                OpeningTrendline = bobMonPos.LastOpenTrendline
            };
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

                    MonBOBTurtlePosition portfPos = new MonBOBTurtlePosition(GetWindow(symbol, true),
                        GetWindow(symbol, false),
                        GetConfig().ExitOnMMov,
                        GetConfig().StopLossForOpenPositionPct,
                        GetConfig().OuterTrendlineSpan,
                        GetConfig().CandleReferencePrice)
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

        #endregion
        
        #region Public Methods
        
        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                ProcessedHistoricalPrices=new List<string>();

//                Trendline._SHORT_SOFT_UPWARD_SLOPE = GetConfig().MaxShortPositiveSlope;
//                Trendline._SHORT_SOFT_DOWNARD_SLOPE = GetConfig().MaxShortNegativeSlope;
//                Trendline._LONG_SOFT_UPWARD_SLOPE = GetConfig().MaxLongPositiveSlope;
//                Trendline._LONG_SOFT_DOWNARD_SLOPE = GetConfig().MaxLongNegativeSlope;

                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);
                
                InitializeManagers(GetConfig().ConnectionString);

                Thread.Sleep(2000);
                
                Thread persistTrendlinesThread = new Thread(new ParameterizedThreadStart(DoPersistTrendlinesThread));
                persistTrendlinesThread.Start();

                Thread delPrevTrehdlinesThread = new Thread(new ParameterizedThreadStart(DeleteAllTrendlines));
                delPrevTrehdlinesThread.Start();

                Thread refreshTrendlinesThread = new Thread(new ParameterizedThreadStart(DoRefreshTrendlines));
                refreshTrendlinesThread.Start();



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
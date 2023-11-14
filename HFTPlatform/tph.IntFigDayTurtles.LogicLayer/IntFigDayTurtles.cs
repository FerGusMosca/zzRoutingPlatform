using System;
using System.Collections.Generic;
using System.Threading;
using tph.DayTurtles.BusinessEntities;
using tph.IntFig.DayTurtles.BusinessEntities;
using tph.IntFig.DayTurtles.Common.Configuration;
using tph.TrendlineTurtles.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Wrappers;


namespace tph.IntFigDayTurtles.LogicLayer
{
   public class IntFigDayTurtles : TrendlineTurtles.LogicLayer.TrendlineTurtles
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

        protected override TradingPosition DoOpenTradingRegularPos(Position pos, PortfolioPosition portfPos)
        {
            MonIntFigTurtlePosition bobPos = (MonIntFigTurtlePosition) portfPos;
            TradTurtlesPosition tradPos= new TradIntFigTurtlesPosition()
            {
                StrategyName = Config.Name,
                OpeningDate = DateTimeManager.Now,
                OpeningPosition = pos,
                OpeningPortfolioPosition = portfPos,
                FeeTypePerTrade = Config.FeeTypePerTrade,
                FeeValuePerTrade = Config.FeeValuePerTrade,
                OpeningTrendline = bobPos.LastOpenTrendline,
                
            };

            return tradPos;
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

                    MonIntFigTurtlePosition portfPos = new MonIntFigTurtlePosition(GetConfig().OpenWindow,
                        GetConfig().CloseWindow,
                        GetConfig().StopLossForOpenPositionPct,
                        GetConfig().OuterTrendlineSpan)
                    {
                        Security = sec,
                        DecimalRounding = Config.DecimalRounding,
                        ProximityPctToTriggerTrade = GetConfig().ProximityPctToTriggerTrade
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

                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);
                
                InitializeManagers(GetConfig().ConnectionString);
                
                Thread.Sleep(2000);
                
                Thread historicalPricesThread = new Thread(new ParameterizedThreadStart(DoRequestHistoricalPricesThread));
                historicalPricesThread.Start();
                
                Thread persistTrendlinesThread = new Thread(new ParameterizedThreadStart(DoPersistTrendlinesThread));
                persistTrendlinesThread.Start();
                
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
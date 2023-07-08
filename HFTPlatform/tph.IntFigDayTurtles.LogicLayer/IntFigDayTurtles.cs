using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.BOBDayTurtles.BusinessEntities;
using tph.DayTurtles.BusinessEntities;
using tph.IntFig.DayTurtles.BusinessEntities;
using tph.IntFig.DayTurtles.Common.Configuration;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.Common.Configuration;
using tph.TrendlineTurtles.DataAccessLayer;
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

namespace tph.IntFigDayTurtles.LogicLayer
{
   public class IntFigayTurtles : TrendlineTurtles.LogicLayer.TrendlineTurtles
    {

        #region Overriden Methods

        public override void DoLoadConfig(string configFile, List<string> noValFlds)
        {
            if (Config == null)
                Config = ConfigLoader.GetConfiguration<Configuration>(this, configFile, noValFlds);
        }

        public TrendlineConfiguration GetConfig()
        {
            return (TrendlineConfiguration) Config;
        }

        #endregion

        #region Protected Methods

        protected override TradingPosition DoOpenTradingRegularPos(Position pos, PortfolioPosition portfPos)
        {
            MonIntFigTurtlePosition bobPos = (MonIntFigTurtlePosition) portfPos;
            return new TradIntFigTurtlesPosition()
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
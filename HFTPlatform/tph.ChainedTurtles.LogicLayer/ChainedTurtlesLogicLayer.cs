using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.Configuration;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Configuration;
using tph.DayTurtles.LogicLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.Common.Wrappers;

namespace tph.ChainedTurtles.LogicLayer
{
    public class ChainedTurtlesLogicLayer : tph.DayTurtles.LogicLayer.DayTurtles
    {

        #region Public Attributes

        public override void DoLoadConfig(string configFile, List<string> noValFlds)
        {
            Config = ConfigLoader.GetConfiguration<ChainedConfiguration>(this, configFile, noValFlds);
        }

        public virtual ChainedConfiguration GetConfig()
        {
            return (ChainedConfiguration)Config;
        }

        #endregion


        #region Overriden Methods

        protected override void LoadMonitorsAndRequestMarketData()
        {
            try
            {

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

                            MonTurtlePosition portfPos = new MonTurtlePosition(
                                GetCustomConfig(security.Symbol),
                                GetConfig().StopLossForOpenPositionPct,
                                GetConfig().CandleReferencePrice)
                            {
                                Security = sec,
                                DecimalRounding = Config.DecimalRounding,
                            };

                            //1- We add the current security to monitor
                            MonitorPositions.Add(security.Exchange, portfPos);

                            Securities.Add(sec);//So far, this is all wehave regarding the Securities

                            //2- No market data to request until Historical Prices are recevied


                        }


                    }


                    //#2- Load monitors for INDICATORS
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
                                                GetCustomConfig(security.Symbol).CloseWindow);

                        i++;
                    }


                    foreach (var indicator in GetConfig().ChainedTurtleIndicators.Where(x=>x.RequestPrices && x.SecurityToMonitor!=null))
                    {

                        DoLog($"@{GetConfig().Name}--> Requesting historical prices for indicator {indicator.SecurityToMonitor.Symbol}", Constants.MessageType.Information);
                        DoRequestHistoricalPrice(i, indicator.SecurityToMonitor.Symbol,
                                                GetCustomConfig(indicator.SecurityToMonitor.Symbol).OpenWindow,
                                                GetCustomConfig(indicator.SecurityToMonitor.Symbol).CloseWindow);

                        i++;

                    }
                
                }


            }
            catch (Exception ex)
            {
                DoLog($"@{Config.Name}--> CRITICAL ERROR requesting historical prices: {ex.Message}", Constants.MessageType.Error);
            
            }
        }

        #endregion

        #region Public Overriden Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;
            StartTime = DateTimeManager.Now;
            LastCounterResetTime = StartTime;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                LoadCustomTurtlesWindows();

                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

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

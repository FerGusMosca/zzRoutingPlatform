using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Configuration;
using tph.DayTurtles.Common.Util;
using tph.DayTurtles.DataAccessLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;

namespace tph.DayTurtles.LogicLayer
{
    public class DayTurtles:DayTradingStrategyBase
    {
        #region Protected Attributes
          
        protected TurtlesPortfolioPositionManager TurtlesPortfolioPositionManager { get; set; }
          
        #endregion
        
        #region Overriden Methods

        public override  void  DoLoadConfig(string configFile, List<string> noValFlds)
        {
            Config = ConfigLoader.GetConfiguration<Configuration>(this, configFile, noValFlds);
        }

        public virtual Configuration GetConfig()
        {
            return (Configuration) Config;
        }
        
        

        #endregion
        
        #region Public Methods

        protected virtual void InitializeManagers()
        {
            TurtlesPortfolioPositionManager= new TurtlesPortfolioPositionManager(GetConfig().ConnectionString);
        }

        protected void EvalOpeningPosition(MonTurtlePosition turtlePos)
        {
            if (turtlePos.LongSignalTriggered())
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) LoadNewPos(turtlePos, Side.Buy);
                PositionWrapper posWrapper = new PositionWrapper(trdPos.OpeningPosition, Config);
                TradingPositions.Add(trdPos.OpeningPosition.Security.Symbol, trdPos);
                CMState state = OrderRouter.ProcessMessage(posWrapper);
                DoLog(string.Format("{0} Position Opened to market. Symbol {1} CashQty={2} DateTime={3} PosId={4} {5}", trdPos.TradeDirection,
                    trdPos.OpeningPosition.Security.Symbol, trdPos.OpeningPosition.CashQty, DateTime.Now, trdPos.OpeningPosition.PosId,turtlePos.SignalTriggered()), Constants.MessageType.Information);

            }
            else if (turtlePos.ShortSignalTriggered())
            {
                if (!Config.OnlyLong)
                {
                    TradTurtlesPosition trdPos = (TradTurtlesPosition) LoadNewPos(turtlePos, Side.Sell);
                    PositionWrapper posWrapper = new PositionWrapper(trdPos.OpeningPosition, Config);
                    TradingPositions.Add(trdPos.OpeningPosition.Security.Symbol, trdPos);
                    CMState state = OrderRouter.ProcessMessage(posWrapper);
                    DoLog(
                        string.Format("{0} Position Opened to market. Symbol {1} CashQty={2} DateTime={3} PosId={4}  {5}",
                            trdPos.TradeDirection, trdPos.OpeningPosition.Security.Symbol,
                            trdPos.OpeningPosition.CashQty, DateTime.Now, trdPos.OpeningPosition.PosId, turtlePos.SignalTriggered()),
                        Constants.MessageType.Information);
                }
                else
                {
                    DoLog(string.Format("SHORT signal for symbol {0} triggered but OnlyLong mode is enabled",turtlePos.Security.Symbol),Constants.MessageType.Information);
                }
            }
            else
            {
                MarketData highest = turtlePos.HighestOnWindow(turtlePos.OpenWindow);
                DoLog(string.Format(
                        "Recv markt data for symbol {0}: LastTrade={1} @{2} - NO SIGNAL TRIGGERED (highest={3})",
                        turtlePos.Security.Symbol, turtlePos.Security.MarketData.Trade, DateTime.Now,
                        highest != null && highest.Trade.HasValue ? highest.Trade.ToString() : "-"),
                    Constants.MessageType.Information);
            }
        }
        
        protected bool EvalClosingPositionOnStopLossHit(MonTurtlePosition turtlePos)
        {
            if (TradingPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) TradingPositions[turtlePos.Security.Symbol];
                
                if(turtlePos.EvalStopLossHit(trdPos))
                {
                    RunClose(trdPos.OpeningPosition, turtlePos, trdPos);
                    DoLog(string.Format("{0} Position Closed on stop loss hit. Symbol {1} Qty={2} DateTime={3} PosId={4}",
                        trdPos.TradeDirection, trdPos.OpeningPosition.Security.Symbol, trdPos.Qty, DateTime.Now,
                        trdPos.OpeningPosition.PosId), Constants.MessageType.Information);
                    return true;
                }
                else
                    return false;
            }

            return false;
        }
        
        protected void EvalAbortingOpeningPositions(MonTurtlePosition turtlePos)
        {
            if (TradingPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) TradingPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == turtlePos.Security.Symbol).FirstOrDefault();
            
                if(turtlePos.EvalAbortingNewLongPosition())
                {
                    CancelRoutingPos(trdPos.OpeningPosition, trdPos);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection, 
                        trdPos.OpeningPosition.Security.Symbol, trdPos.Qty,DateTime.Now,trdPos.OpeningPosition.PosId), Constants.MessageType.Information);
                }

                if(turtlePos.EvalAbortingNewShortPosition())
                {
                    CancelRoutingPos(trdPos.OpeningPosition, trdPos);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection, 
                        trdPos.OpeningPosition.Security.Symbol, trdPos.Qty,DateTime.Now,trdPos.OpeningPosition.PosId), Constants.MessageType.Information);
                }
            }
        }
        
        protected void EvalClosingPosition(MonTurtlePosition turtlePos)
        {
            TradTurtlesPosition trdPos = (TradTurtlesPosition) TradingPositions[turtlePos.Security.Symbol];

            if (trdPos.IsShortDirection() && turtlePos.EvalClosingShortPosition())
            {
                RunClose(trdPos.OpeningPosition, turtlePos, trdPos);
                DoLog(string.Format("Closing {0} Position  on market. Symbol {1} Qty={2} DateTime={3} PosId={4}",
                        trdPos.TradeDirection, trdPos.OpeningPosition.Security.Symbol, trdPos.Qty,
                        DateTime.Now,
                        trdPos.ClosingPosition != null ? trdPos.ClosingPosition.PosId : "-"),
                    Constants.MessageType.Information);
            }
            else if (trdPos.IsLongDirection() && turtlePos.EvalClosingLongPosition())
            {
                RunClose(trdPos.OpeningPosition, turtlePos, trdPos);
                DoLog(string.Format("Closing {0} Position on market. Symbol {1} Qty={2}  DateTime={3} PosId={4}",
                        trdPos.TradeDirection, trdPos.OpeningPosition.Security.Symbol, trdPos.Qty, DateTime.Now,
                        trdPos.ClosingPosition != null ? trdPos.ClosingPosition.PosId : "-"),
                    Constants.MessageType.Information);

            }
            else
            {
                
                MarketData lowest = turtlePos.LowestOnWindow(turtlePos.CloseWindow);
                DoLog(string.Format(
                        "Recv markt data for symbol {0}: LastTrade={1} @{2} - NO CLOSING SIGNAL TRIGGERED (lowest={3})",
                        turtlePos.Security.Symbol, turtlePos.Security.MarketData.Trade, DateTime.Now,
                        lowest != null && lowest.Trade.HasValue ? lowest.Trade.ToString() : "-"),
                    Constants.MessageType.Information);
            }
        }
        
        protected void EvalAbortingClosingPositions(MonTurtlePosition turtlePos)
        {
            if (TradingPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) TradingPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == turtlePos.Security.Symbol).FirstOrDefault();
            
                if(turtlePos.EvalAbortingClosingLongPosition())
                {
                    CancelRoutingPos(trdPos.ClosingPosition, trdPos);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection,
                        trdPos.ClosingPosition.Security.Symbol, trdPos.Qty,DateTime.Now,trdPos.ClosingPosition.PosId), Constants.MessageType.Information);
                }

                if (turtlePos.EvalAbortingClosingShortPosition())
                {
                    CancelRoutingPos(trdPos.ClosingPosition, trdPos);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection, 
                        trdPos.ClosingPosition.Security.Symbol, trdPos.Qty,DateTime.Now,trdPos.ClosingPosition.PosId), Constants.MessageType.Information);
                }
            }
        }
        
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

        protected override TradingPosition DoOpenTradingFuturePos(Position pos, PortfolioPosition portfPos)
        {
            throw new NotImplementedException();
        }

        protected override TradingPosition DoOpenTradingRegularPos(Position pos,PortfolioPosition portfPos)
        {
            return new TradTurtlesPosition()
            {
                StrategyName = Config.Name,
                OpeningDate = DateTime.Now,
                OpeningPosition = pos,
                OpeningPortfolioPosition = portfPos,
                FeeTypePerTrade = Config.FeeTypePerTrade,
                FeeValuePerTrade = Config.FeeValuePerTrade
            };
        }

        protected override void ProcessHistoricalPrices(object pWrapper)
        {
            //We don't need them
        }

        protected override void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper) pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            try
            {
                lock (tLock)
                {
                    string cleanSymbol = SymbolConverter.GetCleanSymbol(md.Security.Symbol);
                    if (PortfolioPositionsToMonitor.Keys.Any(x=>SymbolConverter.GetCleanSymbol(x)==cleanSymbol)&& Securities!=null)
                    //if (PortfolioPositionsToMonitor.ContainsKey(cleanSymbol) && Securities!=null)
                    {
                        string symbol = PortfolioPositionsToMonitor.Keys.Where(x=>SymbolConverter.GetCleanSymbol(x)==cleanSymbol).FirstOrDefault();
                        MonTurtlePosition portfPos = (MonTurtlePosition) PortfolioPositionsToMonitor[symbol];
                        portfPos.AppendCandle(md);
                        EvalOpeningClosingPositions(portfPos);
                        UpdateLastPrice(portfPos, md);
                    }
//                    else
//                    {
//                        DoLog(string.Format("DB-Skipping not monitored MD for symbol {0}",cleanSymbol),Constants.MessageType.Information);
//                    }
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("ERROR @DailyTurtles- Error processing market data:{0}-{1}",e.Message,e.StackTrace),Constants.MessageType.Error);
            }
            

            OrderRouter.ProcessMessage(wrapper);
        }

        protected override void ResetEveryNMinutes(object param)
        {
            //TODO: implement second priority
        }

        protected override void LoadPreviousTradingPositions()
        {
            //TODO: implement  second priority
        }

        protected override void DoPersist(TradingPosition trdPos)
        {
            if (PortfolioPositionsToMonitor.ContainsKey(trdPos.CurrentPos().Security.Symbol))
            {
                lock (tPersistLock)
                {
                    TradTurtlesPosition turtlesTradPos = (TradTurtlesPosition) trdPos;
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

                    MonTurtlePosition portfPos = new MonTurtlePosition(GetConfig().OpenWindow,
                        GetConfig().CloseWindow,
                        GetConfig().StopLossForOpenPositionPct,
                        GetConfig().CandleReferencePrice)
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

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;
            StartTime = DateTime.Now;
            LastCounterResetTime = StartTime;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

                InitializeManagers();
                
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
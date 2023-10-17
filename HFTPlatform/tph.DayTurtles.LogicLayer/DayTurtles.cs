using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Configuration;
using tph.DayTurtles.Common.Util;
using tph.DayTurtles.DataAccessLayer;
using tph.TrendlineTurtles.Common.Configuration;
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
            Config = ConfigLoader.GetConfiguration<DayTurtlesConfiguration>(this, configFile, noValFlds);
        }

        public virtual DayTurtlesConfiguration GetConfig()
        {
            return (DayTurtlesConfiguration) Config;
        }
        
        

        #endregion
        
        
        #region Public Methods


        protected void EvalOpeningPosition(MonTurtlePosition turtlePos)
        {
            if (turtlePos.LongSignalTriggered())
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) LoadNewPos(turtlePos, Side.Buy);
                PositionWrapper posWrapper = new PositionWrapper(trdPos.OpeningPosition, Config);
                PortfolioPositions.Add(trdPos.OpeningPosition.Security.Symbol, trdPos);
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
                    PortfolioPositions.Add(trdPos.OpeningPosition.Security.Symbol, trdPos);
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
            if (PortfolioPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) PortfolioPositions[turtlePos.Security.Symbol];
                
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
            if (PortfolioPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) PortfolioPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == turtlePos.Security.Symbol).FirstOrDefault();
            
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
        
        protected void EvalClosingPosition(MonTurtlePosition monPos)
        {
            TradTurtlesPosition portfPos = (TradTurtlesPosition) PortfolioPositions[monPos.Security.Symbol];

            if (portfPos.IsShortDirection() && monPos.EvalClosingShortPosition()  && !monPos.IsClosing())
            {
                RunClose(portfPos.OpeningPosition, monPos, portfPos);
                DoLog(string.Format("Closing {0} Position  on market. Symbol {1} Qty={2} DateTime={3} PosId={4}",
                        portfPos.TradeDirection, portfPos.OpeningPosition.Security.Symbol, portfPos.Qty,
                        DateTime.Now,
                        portfPos.ClosingPosition != null ? portfPos.ClosingPosition.PosId : "-"),
                    Constants.MessageType.Information);
            }
            else if (portfPos.IsLongDirection() && monPos.EvalClosingLongPosition() && !monPos.IsClosing())
            {
                RunClose(portfPos.OpeningPosition, monPos, portfPos);
                DoLog(string.Format("Closing {0} Position on market. Symbol {1} Qty={2}  DateTime={3} PosId={4}",
                        portfPos.TradeDirection, portfPos.OpeningPosition.Security.Symbol, portfPos.Qty, DateTime.Now,
                        portfPos.ClosingPosition != null ? portfPos.ClosingPosition.PosId : "-"),
                    Constants.MessageType.Information);

            }
            else
            {
                
                MarketData lowest = monPos.LowestOnWindow(monPos.CloseWindow);
//                DoLog(string.Format(
//                        "Recv markt data for symbol {0}: LastTrade={1} @{2} - NO CLOSING SIGNAL TRIGGERED (lowest={3})",
//                        turtlePos.Security.Symbol, turtlePos.Security.MarketData.Trade, DateTime.Now,
//                        lowest != null && lowest.Trade.HasValue ? lowest.Trade.ToString() : "-"),
//                    Constants.MessageType.Information);
            }
        }
        
        protected void EvalAbortingClosingPositions(MonTurtlePosition turtlePos)
        {
            if (PortfolioPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                TradTurtlesPosition trdPos = (TradTurtlesPosition) PortfolioPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == turtlePos.Security.Symbol).FirstOrDefault();
            
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

        protected void EvalDepuratingPositionsThread(object param)
        {

          
            while (true)
            {
                lock (tLock)
                {
                    try 
                    {
                        List<string> toRemove= new List<string>();
                        DoLog($"=========SUMMARY OF PORTF STATUS!=========", Constants.MessageType.Information);
                        foreach (string symbol in PortfolioPositions.Keys)
                        {
                            TradingPosition portfPos = PortfolioPositions[symbol];

                            DoLog($"Summary for Symbol={symbol} FirstLeg={portfPos.IsFirstLeg()} LongCumQty={portfPos.OpenCumQty()} CloseCumQty={portfPos.CloseCumQty()}",Constants.MessageType.Information);


                            if (   portfPos.OpeningPosition != null 
                                && portfPos.OpeningPosition.PositionNoLongerActive()
                                && portfPos.ClosingPosition!=null
                                && portfPos.ClosingPosition.PositionNoLongerActive()
                                && portfPos.IsInFactClosedPortfPos()
                                )
                            {
                                toRemove.Add(symbol);
                            }


                        }

                        foreach (string symbol in toRemove)
                        {
                            DoLog($"Portf Poisition removed cuz considered closed: {symbol} ", Constants.MessageType.Information);
                            PortfolioPositions.Remove(symbol);
                        
                        }
                    
                    
                    
                    }
                    catch(Exception ex)
                    {
                        DoLog($"CRITICAL error removing closed positions with unperfect closing:{ex.Message}", Constants.MessageType.Error);
                    }
                }



                Thread.Sleep(60 * 1000);//1 minute
            }

        }
        
        protected void EvalOpeningClosingPositions(MonTurtlePosition monPos)
        {
            
            TimeSpan elapsed = DateTime.Now - StartTime;

            if (PortfolioPositions.Keys.Count < Config.MaxOpenedPositions 
                && !PortfolioPositions.ContainsKey(monPos.Security.Symbol)
                && IsTradingTime()
            )
            {
                EvalOpeningPosition(monPos);
            }
            else if (PortfolioPositions.ContainsKey(monPos.Security.Symbol) && IsTradingTime())
            {
                EvalClosingPosition(monPos);
                EvalClosingPositionOnStopLossHit(monPos);
                EvalAbortingOpeningPositions(monPos);
                EvalAbortingClosingPositions(monPos);
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

        public override void InitializeManagers(string connStr)
        {
            TurtlesPortfolioPositionManager = new TurtlesPortfolioPositionManager(connStr);
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
                    TradTurtlesPosition portfPos = (TradTurtlesPosition) trdPos;
                    TurtlesPortfolioPositionManager.PersistPortfolioPositionTrade(portfPos);
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
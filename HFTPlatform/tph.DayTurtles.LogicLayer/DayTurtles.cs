using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Configuration;
using tph.DayTurtles.Common.Util;
using tph.DayTurtles.DataAccessLayer;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;

namespace tph.DayTurtles.LogicLayer
{
    public class DayTurtles : DayTradingStrategyBase
    {
        #region Protected Attributes

        protected TurtlesPortfolioPositionManager TurtlesPortfolioPositionManager { get; set; }

        protected TurtlesCustomWindowManager TurtlesCustomWindowManager { get; set; }


        protected List<TurtlesCustomConfig> TurtlesWindowList { get; set; }

        #endregion

        #region Overriden Methods

        public override void DoLoadConfig(string configFile, List<string> noValFlds)
        {
            Config = ConfigLoader.GetConfiguration<DayTurtlesConfiguration>(this, configFile, noValFlds);
        }

        public virtual DayTurtlesConfiguration GetConfig()
        {
            return (DayTurtlesConfiguration)Config;
        }



        #endregion


        #region Public Methods

        protected void DoRequestHistoricalPrice(int i,string symbol, int openWindow, int closeWindow)
        {

            
            int windowToUse = openWindow > closeWindow ? openWindow : closeWindow;

            DateTime from = DateTimeManager.Now.AddDays(-1);
            DateTime to = DateTimeManager.Now;

            HistoricalPricesRequestWrapper reqWrapper = new HistoricalPricesRequestWrapper(i, symbol, from, to, CandleInterval.Minute_1);
            OnMessageRcv(reqWrapper);
        }

        protected virtual void DoRequestHistoricalPrice(int i, string symbol, int window,string currency, 
                                                SecurityType? pSecurityType,string exchange)
        {


          

            DateTime from = DateTimeManager.Now.AddDays(window);
            DateTime to = DateTimeManager.Now;

            HistoricalPricesRequestWrapper reqWrapper = new HistoricalPricesRequestWrapper(i, symbol, from, to, 
                                                                                            CandleInterval.Minute_1,
                                                                                            currency,pSecurityType,exchange);
            OnMessageRcv(reqWrapper);
        }

        protected override void DoRequestHistoricalPricesThread(object param)
        {
            try
            {
                int i = 1;

                foreach (string symbol in MonitorPositions.Keys)
                {

                    DoRequestHistoricalPrice(i,symbol, GetCustomConfig(symbol).OpenWindow, GetCustomConfig(symbol).CloseWindow);
                    i++;
                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("@BOBDayTurtles - Critical ERROR Requesting Historical Prices:{0}", e.Message), Constants.MessageType.Error);
            }
        }

        protected PositionWrapper OpenRoutingPos(MonTurtlePosition monPos, Side side)
        {
            PortfTurtlesPosition trdPos = (PortfTurtlesPosition)LoadNewPos(monPos, side);
            PositionWrapper posWrapper = new PositionWrapper(trdPos.OpeningPosition, Config);

            bool added = PortfolioPositions.TryAdd(trdPos.OpeningPosition.Security.Symbol, trdPos);
            if (!added)
                throw new Exception($"Could not add symbol {trdPos.OpeningPosition.Security.Symbol} to the PortfolioPositions dictionary because it already exists!!!!! ");

            DoLog(string.Format("{0} Position Opened to market. Symbol {1} CashQty={2} DateTime={3} PosId={4} {5}",
                trdPos.TradeDirection,
                trdPos.OpeningPosition.Security.Symbol,
                trdPos.OpeningPosition.CashQty,
                DateTimeManager.Now,
                trdPos.OpeningPosition.PosId,
                monPos.SignalTriggered()),
                Constants.MessageType.Information);

            return posWrapper;
        }


        protected void EvalOpeningPosition(MonTurtlePosition monPos)
        {
            if (monPos.LongSignalTriggered())
            {
                PositionWrapper posWrapper = OpenRoutingPos(monPos, Side.Buy);
                OrderRouter.ProcessMessage(posWrapper);

            }
            else if (monPos.ShortSignalTriggered())
            {
                if (!Config.OnlyLong)
                {
                    PositionWrapper posWrapper = OpenRoutingPos(monPos, Side.Sell);
                    OrderRouter.ProcessMessage(posWrapper);
                }
                else
                {
                    DoLog(string.Format("SHORT signal for symbol {0} triggered but OnlyLong mode is enabled", monPos.Security.Symbol), Constants.MessageType.Information);
                }
            }
            else
            {
                DoLog($"Recv markt data for symbol {monPos.Security.Symbol}: LastTrade={monPos.Security.MarketData.Trade} @{DateTimeManager.Now} - NO SIGNAL TRIGGERED",Constants.MessageType.Information);
                DoLog($"Inner Pos Info for {monPos.Security.Symbol}:{monPos.RelevantInnerInfo()}", Constants.MessageType.Information);
            }
        }

        protected bool EvalClosingPositionOnStopLossHit(MonTurtlePosition turtlePos)
        {
            if (PortfolioPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                PortfTurtlesPosition trdPos = (PortfTurtlesPosition)PortfolioPositions[turtlePos.Security.Symbol];

                if (turtlePos.EvalStopLossHit(trdPos))
                {
                    RunClose(trdPos.OpeningPosition, turtlePos, trdPos);
                    DoLog(string.Format("{0} Position Closed on stop loss hit. Symbol {1} Qty={2} DateTime={3} PosId={4}",
                        trdPos.TradeDirection, trdPos.OpeningPosition.Security.Symbol, trdPos.Qty, DateTimeManager.Now,
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
                PortfTurtlesPosition trdPos = (PortfTurtlesPosition)PortfolioPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == turtlePos.Security.Symbol).FirstOrDefault();

                if (turtlePos.EvalAbortingNewLongPosition())
                {
                    CancelRoutingPos(trdPos.OpeningPosition, trdPos);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection,
                        trdPos.OpeningPosition.Security.Symbol, trdPos.Qty, DateTimeManager.Now, trdPos.OpeningPosition.PosId), Constants.MessageType.Information);
                }

                if (turtlePos.EvalAbortingNewShortPosition())
                {
                    CancelRoutingPos(trdPos.OpeningPosition, trdPos);
                    DoLog(string.Format("{0} Aborting opening position to market. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection,
                        trdPos.OpeningPosition.Security.Symbol, trdPos.Qty, DateTimeManager.Now, trdPos.OpeningPosition.PosId), Constants.MessageType.Information);
                }
            }
        }

        protected void CloseAllOpenPositions(MonTurtlePosition monPos)
        {

            if (PortfolioPositions.Values.Any(x => x.OpeningPosition != null && x.OpeningPosition.Security.Symbol == monPos.Security.Symbol))
            {
                PortfolioPosition portfPos = PortfolioPositions.Values.Where(x => x.OpeningPosition != null
                                                                                && x.OpeningPosition.Security.Symbol == monPos.Security.Symbol)
                                                                    .FirstOrDefault();
                if (!monPos.IsClosing() && portfPos.ClosingPosition == null)
                {
                    DoLog(string.Format("Closing {0} Position on market CLOSED. Symbol {1} Qty={2} DateTime={3} PosId={4} Signal={5}",
                           portfPos.TradeDirection, portfPos.OpeningPosition.Security.Symbol, portfPos.Qty,
                           DateTimeManager.Now,
                           portfPos.ClosingPosition != null ? portfPos.ClosingPosition.PosId : "-",
                           "MARKET CLOSED"),
                       Constants.MessageType.Information);
                    RunClose(portfPos.OpeningPosition, monPos, portfPos);


                }
                else
                {
                    string posId = portfPos.ClosingPosition != null ? portfPos.ClosingPosition.PosId : "-";
                    DoLog($"WAITING CLOSING {portfPos.TradeDirection} Position on market CLOSED. Symbol {portfPos.OpeningPosition.Security.Symbol} Qty={portfPos.Qty} DateTime={DateTimeManager.Now} PosId={posId} Signal=ON CLOSING ON MARKET CLOSED",
                           Constants.MessageType.Information);
                }
            }
        }

        protected void EvalClosingPosition(MonTurtlePosition monPos)
        {
            PortfTurtlesPosition portfPos = (PortfTurtlesPosition)PortfolioPositions[monPos.Security.Symbol];

            if (monPos.EvalClosingShortPosition(portfPos) && !monPos.IsClosing())
            {
                DoLog(string.Format("Closing {0} Position on market. Symbol {1} Qty={2} DateTime={3} PosId={4} Signal={5}",
                        portfPos.TradeDirection, portfPos.OpeningPosition.Security.Symbol, portfPos.Qty,
                        DateTimeManager.Now,
                        portfPos.ClosingPosition != null ? portfPos.ClosingPosition.PosId : "-",
                        monPos.SignalTriggered()),
                    Constants.MessageType.Information);
                RunClose(portfPos.OpeningPosition, monPos, portfPos);
            }
            else if ( monPos.EvalClosingLongPosition(portfPos) && !monPos.IsClosing())
            {
                DoLog(string.Format("Closing {0} Position on market. Symbol {1} Qty={2}  DateTime={3} PosId={4} Signal={5}",
                        portfPos.TradeDirection, portfPos.OpeningPosition.Security.Symbol, portfPos.Qty, DateTimeManager.Now,
                        portfPos.ClosingPosition != null ? portfPos.ClosingPosition.PosId : "-",
                        monPos.SignalTriggered()),
                    Constants.MessageType.Information);
                RunClose(portfPos.OpeningPosition, monPos, portfPos);

            }
            else
            { 
                double? price = ReferencePriceCalculator.GetReferencePrice(monPos.GetLastFinishedCandle(), GetConfig().CandleReferencePrice);
                string currPrice= price.HasValue ? price.Value.ToString("#.##") : "";
                DoLog($"{Config.Name} -> @{DateTimeManager.Now} (Price={currPrice}) " +
                         "NO CLOSING SIGNAL TRIGGERED triggered for symbol " +
                         $"{monPos.Security.Symbol} (Side={portfPos.OpeningPosition.Side} TriggerPrice={portfPos.OpeningPosition.GetTriggerPrice()}) " +
                         $"(MMov={monPos.CalculateSimpleMovAvg().ToString("#.##")}) ",
                         Constants.MessageType.Information);
            }
        }

        protected void EvalAbortingClosingPositions(MonTurtlePosition turtlePos)
        {
            if (PortfolioPositions.ContainsKey(turtlePos.Security.Symbol))
            {
                PortfTurtlesPosition trdPos = (PortfTurtlesPosition)PortfolioPositions.Values.Where(x => x.OpeningPosition.Security.Symbol == turtlePos.Security.Symbol).FirstOrDefault();

                if (turtlePos.EvalAbortingClosingLongPosition())
                {
                    CancelRoutingPos(trdPos.ClosingPosition, trdPos);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection,
                        trdPos.ClosingPosition.Security.Symbol, trdPos.Qty, DateTimeManager.Now, trdPos.ClosingPosition.PosId), Constants.MessageType.Information);
                }

                if (turtlePos.EvalAbortingClosingShortPosition())
                {
                    CancelRoutingPos(trdPos.ClosingPosition, trdPos);
                    DoLog(string.Format("{0} Aborting closing position. Symbol {1} Qty={2} DateTime={3} Pos={4}", trdPos.TradeDirection,
                        trdPos.ClosingPosition.Security.Symbol, trdPos.Qty, DateTimeManager.Now, trdPos.ClosingPosition.PosId), Constants.MessageType.Information);
                }
            }
        }

        protected void EvalDepuratingPositionsThread(object param)
        {
            while (true)
            {
               
                try
                {
                    List<string> toRemove = new List<string>();
                    DoLog($"=========SUMMARY OF PORTF STATUS!=========", Constants.MessageType.Information);
                    foreach (string symbol in PortfolioPositions.Keys)
                    {
                        PortfolioPosition portfPos = PortfolioPositions[symbol];

                        DoLog($"Summary for Symbol={symbol} FirstLeg={portfPos.IsFirstLeg()} LongCumQty={portfPos.OpenCumQty()} CloseCumQty={portfPos.CloseCumQty()}", Constants.MessageType.Information);


                        if (portfPos.OpeningPosition != null
                            && portfPos.OpeningPosition.PositionNoLongerActive()
                            && portfPos.ClosingPosition != null
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
                        PortfolioPositions.TryRemove(symbol,out _);

                    }

                }
                catch (Exception ex)
                {
                    DoLog($"CRITICAL error removing closed positions with unperfect closing:{ex.Message}", Constants.MessageType.Error);
                }

                Thread.Sleep(60 * 1000);//1 minute
            }

        }



        protected void EvalOpeningClosingPositions(MonTurtlePosition monPos)
        {

            if (IsTradingTime() && !TradingStarted)//First Candle
            {
                TradingStarted = true;
                StartTime = DateTimeManager.Now;
                EndTime = null;
            }

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
            else if (!IsTradingTime())
            {
                CloseTradingDay();
                CloseAllOpenPositions(monPos);
            }
        }

        protected override PortfolioPosition DoOpenTradingFuturePos(Position pos, MonitoringPosition portfPos)
        {
            throw new NotImplementedException();
        }

        protected override PortfolioPosition DoOpenTradingRegularPos(Position pos, MonitoringPosition portfPos)
        {
            return new PortfTurtlesPosition()
            {
                StrategyName = Config.Name,
                OpeningDate = DateTimeManager.Now,
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

        public void LoadCustomTurtlesWindows()
        {
            TurtlesCustomWindowManager = new TurtlesCustomWindowManager(GetConfig().ConnectionString);

            TurtlesWindowList = TurtlesCustomWindowManager.GetTurtlesCustomWindow();
        }

        protected override void ProcessHistoricalPrices(object pWrapper)
        {
            try
            {
               
                LoadHistoricalPrices((HistoricalPricesWrapper)pWrapper);
                
            }
            catch (Exception e)
            {
                DoLog(string.Format("Critical ERROR processing Trendlines : {0}", e.Message),
                    Constants.MessageType.Error);
            }
        }
    

        protected override void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper)pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);
            DateTimeManager.NullNow = md.GetReferenceDateTime();
            OrderRouter.ProcessMessage(wrapper);
            try
            {
                lock (tLock)
                {
                    
                    string cleanSymbol = SymbolConverter.GetCleanSymbol(md.Security.Symbol);
                    if (MonitorPositions.Keys.Any(x => SymbolConverter.GetCleanSymbol(x) == cleanSymbol) && Securities != null)
                    //if (PortfolioPositionsToMonitor.ContainsKey(cleanSymbol) && Securities!=null)
                    {
                        string symbol = MonitorPositions.Keys.Where(x => SymbolConverter.GetCleanSymbol(x) == cleanSymbol).FirstOrDefault();
                        MonTurtlePosition portfPos = (MonTurtlePosition)MonitorPositions[symbol];
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
                DoLog(string.Format("ERROR @DailyTurtles- Error processing market data:{0}-{1}", e.Message, e.StackTrace), Constants.MessageType.Error);
            }


            
        }

        protected override void ResetEveryNMinutes(object param)
        {
            //TODO: implement second priority
        }

        protected override void LoadPreviousTradingPositions()
        {
            //TODO: implement  second priority
        }

        protected override void DoPersist(PortfolioPosition trdPos)
        {
            if (MonitorPositions.ContainsKey(trdPos.CurrentPos().Security.Symbol))
            {
                lock (tPersistLock)
                {
                    PortfTurtlesPosition portfPos = (PortfTurtlesPosition)trdPos;
                    TurtlesPortfolioPositionManager.Persist(portfPos);
                }

            }
        }

        protected TurtlesCustomConfig GetCustomConfig(string symbol)
        {

            if (TurtlesWindowList.Any(x => x.Symbol == symbol))
            {

                return TurtlesWindowList.Where(x => x.Symbol == symbol).FirstOrDefault();

            }
            else
                throw new Exception($"Not found custom config for symbol {symbol}");
        }
        
        protected override void LoadMonitorsAndRequestMarketData()
        {
            Thread.Sleep(5000);
            foreach (string symbol in Config.StocksToMonitor)
            {
                if (!MonitorPositions.ContainsKey(symbol))
                {
                    Security sec = new Security()
                    {
                        Symbol = symbol,
                        SecType = Security.GetSecurityType(Config.SecurityTypes),
                        MarketData = new MarketData() { SettlType = SettlType.Tplus2 },
                        Currency = Config.Currency,
                        Exchange = Config.Exchange
                    };

                    MonTurtlePosition portfPos = new MonTurtlePosition(
                        GetCustomConfig(symbol),
                        GetConfig().StopLossForOpenPositionPct,
                        GetConfig().CandleReferencePrice)
                    {
                        Security = sec,
                        DecimalRounding = Config.DecimalRounding,
                    };

                    //1- We add the current security to monitor
                    MonitorPositions.Add(symbol, portfPos);

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
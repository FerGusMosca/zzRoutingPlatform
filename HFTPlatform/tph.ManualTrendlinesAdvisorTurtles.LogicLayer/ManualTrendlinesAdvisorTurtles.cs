using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.BOBDayTurtles.BusinessEntities;
using tph.ManualTrendlinesAdvisorTurtles.Common.Configuration;
using tph.ManualTrendlinesAdvisorTurtles.Yahoo.Common;
using tph.ManualTrendlinesAdvisorTurtles.Yahoo.Common.DTO;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.DataAccessLayer;
using tph.TrendlineTurtles.LogicLayer.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandler.BusinessEntities;
using zHFT.StrategyHandler.Common.Wrappers;

namespace tph.ManualTrendlinesAdvisorTurtles.LogicLayer
{
    public class ManualTrendlinesAdvisorTurtles : BOBDayTurtles.LogicLayer.BOBDayTurtles
    {

        # region Protected Attributes

            protected YahooTokenServiceClient YahooTokenServiceClient { get; set; }

            protected YahooPricesServiceClient YahooPricesServiceClient { get; set; }

            #endregion

        #region Overriden Methods

        public override void DoLoadConfig(string configFile, List<string> noValFlds)
        {
            if (Config == null)
                Config = ConfigLoader.GetConfiguration<AdvisorConfiguration>(this, configFile, noValFlds);
        }

        public AdvisorConfiguration GetConfig()
        {
            return (AdvisorConfiguration)Config;
        }

        #endregion


        #region Protected Overriden Methods

        protected override void DoRequestHistoricalPricesThread(object param) 
        {
            try
            {
                TrendlineManager mgr = new TrendlineManager(GetConfig().ConnectionString);
                foreach (string symbol in Config.StocksToMonitor)
                {

                    MonTrendlineTurtlesPosition monPos = (MonTrendlineTurtlesPosition) MonitorPositions.Values.Where(x=>x.Security.Symbol==symbol).FirstOrDefault();

                    if (monPos == null)
                        throw new Exception($"CRITICAL ERROR --> Could not find position :{symbol}");

                    List<Trendline>  trendlines = mgr.GetTrendlines();
                    Trendline firstTrendline = trendlines.Where(x => x.Symbol == symbol).OrderByDescending(x => x.StartDate).FirstOrDefault();
                    DateTime minDate = firstTrendline != null ? firstTrendline.StartDate : DateTime.Now;

                    bool success = false;
                    int i = 0;

                    DoLog($"Downloading historical prices for symbol {symbol}", Constants.MessageType.Information);
                    while (!success)
                    {
                        try
                        {
                            YahooTokenServiceClient.Refresh();

                            YahooPricesServiceClient = new YahooPricesServiceClient(GetConfig().YahooPricesDownloadURL, YahooTokenServiceClient.Cookie, YahooTokenServiceClient.Crumb);

                            List<Price> prices = YahooPricesServiceClient.GetPrices(symbol + GetConfig().YahooPostfix, minDate.AddDays(-10), DateTime.Now) ;
                            DoLog($"Found {prices.Count} prices for {symbol} since date {minDate.AddDays(-10)} ", Constants.MessageType.Information);

                            foreach (Price price in prices)
                            {

                                
                                MarketData currCandle = new MarketData()
                                {
                                    Security = new Security() { Symbol = symbol },
                                    OpeningPrice = Convert.ToDouble(price.Open),
                                    TradingSessionHighPrice = Convert.ToDouble(price.Maximum),
                                    TradingSessionLowPrice = Convert.ToDouble(price.Minimum),
                                    ClosingPrice = Convert.ToDouble(price.AdjClose),
                                    MDEntryDate = price.Date,
                                    MDLocalEntryDate = price.Date,
                                    NominalVolume = price.Volume
                                };


                                DoLog($"Appending candle {currCandle.ToString()} for symbol {symbol}", Constants.MessageType.Information);
                                monPos.AppendCandle(currCandle);
                            }

                            DoLog($"All candles added for symbol {symbol}", Constants.MessageType.Information);
                            
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            YahooTokenServiceClient.Cookie = null;
                            YahooTokenServiceClient.Crumb = null;
                            i++;

                            if (i >= 10)
                                throw ex;
                        }
                    }
                }



            }
            catch (Exception e) {

                DoLog(string.Format("CRITICAL ERROR downloading historical prices :{0}-{1}", e.Message, e.StackTrace),Constants.MessageType.Error);

            }
        
        
        }

        protected override void DoRequestSecurityListThread(object param) { }

        protected override void DeleteAllTrendlines(object param) {}

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

                    MonBOBTurtlePosition portfPos = new MonBOBTurtlePosition(
                        GetCustomConfig(symbol),
                        GetConfig().StopLossForOpenPositionPct,
                        GetConfig().OuterTrendlineSpan,
                        GetConfig().CandleReferencePrice)
                    {
                        Security = sec,
                        DecimalRounding = Config.DecimalRounding,
                    };

                    //1- We add the current security to monitor
                    MonitorPositions.Add(symbol, portfPos);

                    Securities.Add(sec); //So far, this is all wehave regarding the Securities

                    TrendLineCreator.InitializeCreator(sec, GetConfig(),
                                                   DateTimeManager.Now.AddDays(GetConfig().HistoricalPricesPeriod),
                                                   DoLog,
                                                   _REPEATED_MAX_MIN_MAX_DISTANCE,
                                                   _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE);

                    //2- We request market data --> TODO properly request Market data to the right module

                    MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper(MarketDataRequestCounter, sec,
                        SubscriptionRequestType.SnapshotAndUpdates);
                    MarketDataRequestCounter++;
                    OrderRouter.ProcessMessage(wrapper);
                    //OnMessageRcv(wrapper);
                }
            }
        }

        protected override async void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper)pWrapper;
            MarketData md = MarketDataConverter.GetMarketData(wrapper, Config);

            try
            {
                lock (tLock)
                {
                    DateTimeManager.NullNow = md.GetReferenceDateTime();
                    if (MonitorPositions.ContainsKey(md.Security.Symbol))
                    {
                        MonTrendlineTurtlesPosition monPos = (MonTrendlineTurtlesPosition)MonitorPositions[md.Security.Symbol];
                        
                        if (monPos.HasHistoricalCandles())
                        {
                            bool newCandle = monPos.AppendCandle(md);
                            if (newCandle)
                            {
                                EvalBrokenTrendlines(monPos, md);

                                if (monPos.Resistances.Any(x => x.JustBroken))
                                {
                                    Trendline justBroken = monPos.Resistances.Where(x => x.JustBroken).FirstOrDefault();
                                    DoLog($"FOUND NEW BROKEN RESISTANCE for symbol {monPos.Security.Symbol}: Trendline={justBroken.GetBrokenData()}!!", Constants.MessageType.PriorityInformation);
                                }

                                if (monPos.Supports.Any(x => x.JustBroken))
                                {
                                    Trendline justBroken = monPos.Resistances.Where(x => x.JustBroken).FirstOrDefault();
                                    DoLog($"FOUND NEW BROKEN SUPPORT  for symbol {monPos.Security.Symbol}: Trendline={justBroken.GetBrokenData()}!!", Constants.MessageType.PriorityInformation);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                DoLog(
                    string.Format("ERROR @ManualTrendlinesAdvisorTurtles- Error processing market data:{0}-{1}", e.Message, e.StackTrace),
                    Constants.MessageType.Error);
            }
        }


        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
              
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    Thread ProcessExecutionReportThread = new Thread(new ParameterizedThreadStart(ProcessMarketData));
                    ProcessExecutionReportThread.Start(wrapper);
                    return CMState.BuildSuccess();
                }
                else 
                {
                    return base.ProcessOutgoing(wrapper);
                }
                
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }

        }


        #endregion


        #region Public Methods


        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                try
                {

                    DoLog($"Initializing Manual Trendlines Turtles advisor @{DateTimeManager.Now}", Constants.MessageType.Information);
                    ProcessedHistoricalPrices = new List<string>();
                    YahooTokenServiceClient = new YahooTokenServiceClient(GetConfig().YahooTokenURL);
                    
                    base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

                    InitializeManagers(GetConfig().ConnectionString);

                    Thread.Sleep(2000);

                    Thread refreshTrendlinesThread = new Thread(new ParameterizedThreadStart(DoRefreshTrendlines));
                    refreshTrendlinesThread.Start();

                    DoLog($"Successfully initialized Manual Trendline Advisor!", Constants.MessageType.Information);
                    return true;
                }
                catch(Exception ex)
                {
                    DoLog($"CRITICAL ERROR Initializing Manual Trendline Advisot:{ex.Message}", Constants.MessageType.Error);


                    return false;
                }

            }
            else
            {
                DoLog($"CRITICAL ERROR Initializing Manual Trendline Advisot:Could not load config file!", Constants.MessageType.Error);
                
                return false;
            }
        }

        #endregion

    }
}

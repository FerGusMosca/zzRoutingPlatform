using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using tph.MarketClient.Mock.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.MarketClient.Common.Converters;
using zHFT.MarketClient.Common.DTO;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.DataAccessLayer;

namespace tph.MarketClient.Mock
{
    public class MarketClient : MarketClientBase, ICommunicationModule
    {

        #region Protected Attributes

        protected Configuration Configuration { get; set; }

        protected CandleManager CandleManager { get; set; }

        protected object tObject { get; set; }

        protected Dictionary<string, bool> MarketDataUnsubscribeFlagDict { get; set; }


        protected Dictionary<string, bool> HistoricalPricesFinishedDict { get; set; }

        #endregion


        #region Protected Static Consts

        private static int _MIN_CANDLES_FOR_DAY = 10;

        #endregion


        #region Protected Methods

        protected void DoPublishMarketDataAync(object param)
        {
            try
            {
                object[] paramList = (object[])param;
                Queue<Wrapper> mdWrappperQueue = (Queue<Wrapper>)paramList[0];


                while(mdWrappperQueue.Count>0)
                //foreach (Wrapper md in mdWrappperQueue)
                {
                    Wrapper md = mdWrappperQueue.Dequeue();
                    DoLog($"{Configuration.Name}: Publishing Market Data:{md.ToString()}", Constants.MessageType.Information);

                    (new Thread(OnPublishAsync)).Start(md);
                    Thread.Sleep(Configuration.PacingMarketDataMilliSec);

                    string symbol = (string) md.GetField(MarketDataFields.Symbol);
                    if (EvalUnsubscription(symbol))
                    {
                        DoLog($"Unbubscribing market data for symbol {symbol}", Constants.MessageType.PriorityInformation);
                        break;
                    }
                }

                DoLog($"============= ALL Market Data successfully sent =============", Constants.MessageType.Information);

            }
            catch (Exception ex)
            {

                DoLog($"CRITICAL ERROR Publishing Market Data!:{ex.Message}", Constants.MessageType.Error);
            }
        }

        private bool EvalUnsubscription(string symbol)
        {
            lock (MarketDataUnsubscribeFlagDict)
            {
                if (!MarketDataUnsubscribeFlagDict.ContainsKey(symbol))
                {
                    MarketDataUnsubscribeFlagDict.Add(symbol, false);
                }

                return MarketDataUnsubscribeFlagDict[symbol];
            }
        }

        protected DateTime GetFrom()
        {
            return DateTimeManager.Now;
        }

        protected DateTime GetTo()
        {
            return DateTimeManager.Now.AddDays(1);
        }

        protected void SetMarketClosingTime(List<MarketData> candles)
        {
            MarketData lastCandle = candles.OrderByDescending(x => x.GetReferenceDateTime().Value).FirstOrDefault();

            if (lastCandle != null)
            {
                if (Configuration.ClosingMinutesBeforeLastCandle.HasValue && lastCandle.GetReferenceDateTime().HasValue)
                {
                    ClosingTimeManager.ClosingTime = lastCandle.GetReferenceDateTime().Value.AddMinutes(-1 * Configuration.ClosingMinutesBeforeLastCandle.Value);

                }
                else
                {
                    ClosingTimeManager.ClosingTime = null;

                }
            }
        }

        private DateTime FindLatestStartTime(Dictionary<string, List<MarketData>> candlesDict)
        {
            DateTime? latesttStart = null; 
            foreach (string symbol in candlesDict.Keys)
            {
                if (!latesttStart.HasValue)
                {
                    if (candlesDict[symbol].Count > 0)// In case we are running this on weekends or holidays
                    {
                        latesttStart = candlesDict[symbol].Where(x => x.GetReferenceDateTime() != null)
                                                         .OrderBy(x => x.GetReferenceDateTime()).FirstOrDefault()
                                                         .GetReferenceDateTime();
                    }
                    else
                    {
                        latesttStart = DateTime.MaxValue;
                    }

                }
                else

                {
                    if (candlesDict[symbol].Count > 0)
                    {
                        DateTime? currSymbolLatestStart = candlesDict[symbol].Where(x => x.GetReferenceDateTime() != null)
                                                         .OrderBy(x => x.GetReferenceDateTime()).FirstOrDefault()
                                                         .GetReferenceDateTime();

                        if (currSymbolLatestStart.HasValue && DateTime.Compare(currSymbolLatestStart.Value, latesttStart.Value) > 0)
                        {
                            latesttStart = currSymbolLatestStart;
                        }
                    }
                }
            }


            if (!latesttStart.HasValue)
                throw new Exception($"Could not calculate the Latest Start Time on Market Data Request Bulk for {candlesDict.Keys.Count} securities!. Potentially missing market data");
            else 
                return latesttStart.Value;
        }

        private DateTime FindEarliestEndTime(Dictionary<string, List<MarketData>> candlesDict)
        {
            DateTime? earliestEnd = null;
            foreach (string symbol in candlesDict.Keys)
            {
                if (!earliestEnd.HasValue)
                {
                    if (candlesDict[symbol].Count > 0)// In case we are running this on weekends or holidays
                    {
                        earliestEnd = candlesDict[symbol].Where(x => x.GetReferenceDateTime() != null)
                                                         .OrderByDescending(x => x.GetReferenceDateTime()).FirstOrDefault()
                                                         .GetReferenceDateTime();
                    }
                    else
                    {
                        earliestEnd = DateTime.MinValue;
                    }

                }
                else

                {
                    if (candlesDict[symbol].Count > 0)
                    {
                        DateTime? currSymbolEarliestEnd = candlesDict[symbol].Where(x => x.GetReferenceDateTime() != null)
                                                     .OrderByDescending(x => x.GetReferenceDateTime()).FirstOrDefault()
                                                     .GetReferenceDateTime();

                        if (currSymbolEarliestEnd.HasValue && DateTime.Compare(currSymbolEarliestEnd.Value, earliestEnd.Value) < 0)
                        {
                            earliestEnd = currSymbolEarliestEnd;
                        }
                    }
                }
            }

            if (!earliestEnd.HasValue)
                throw new Exception($"Could not calculate the Earliest End Time on Market Data Request Bulk for {candlesDict.Keys.Count} securities!. Potentially missing market data");
            else
                return earliestEnd.Value;
        }


        private void DepurateTimeDiffs(Dictionary<string, List<MarketData>> candlesDict, DateTime from, DateTime to)
        {

            try
            {
                //We fetch the lowest start time
                DateTime lowestStartTime = FindLatestStartTime(candlesDict);
                DateTime earliestEndTime = FindEarliestEndTime(candlesDict);


                List<string> symbols = new List<string>(candlesDict.Keys);
                foreach (string symbol in symbols)
                { 
                    List<MarketData> candles = candlesDict[symbol];

                    candles = candles.Where(x => x.GetReferenceDateTime() != null &&
                                               DateTime.Compare(lowestStartTime, x.GetReferenceDateTime().Value) <= 0 &&
                                               DateTime.Compare(x.GetReferenceDateTime().Value, earliestEndTime) <= 0).ToList();

                    candlesDict[symbol] = candles;

                    //We filtered the out of range dates/candles
                }


            }
            catch (Exception ex)
            {
                DoLog($"Missing market data for date {from}/{to}.Potential weekend/holiday", Constants.MessageType.Information);

            }

        }


        private List<MarketData> ResuhffleMultiSecMarketData(Dictionary<string, List<MarketData>> candlesDict)
        {

            List<MarketData> allCandles = new List<MarketData>();

            foreach (List<MarketData> symbolCandles in candlesDict.Values)
            {
                allCandles.AddRange(symbolCandles);
            }

            List<MarketData> ordCandles= allCandles.OrderBy(x => x.GetReferenceDateTime()).ThenBy(x=>x.Security.RankSecurityType()).ToList();

            return ordCandles;
        }

        private List<MarketData> ExtractAllMarketDataBulk(Security[] securities, bool depurateTimeDiffs)
        {
            DateTime from = GetFrom();
            DateTime to = GetTo();
            Dictionary<string, List<MarketData>> candlesDict = new Dictionary<string, List<MarketData>>();


            foreach (Security sec in securities)
            {
                List<MarketData> candles = CandleManager.GetCandles(sec.Symbol, CandleInterval.Minute_1, from, to);
                candles.ForEach(x => x.Security = sec);
                candlesDict.Add(sec.Symbol, candles);
            }

            if (depurateTimeDiffs)
                DepurateTimeDiffs(candlesDict, from, to);

            return ResuhffleMultiSecMarketData(candlesDict);
        }

        protected void DoPublish(List<MarketData> candles)
        {
            Queue<Wrapper> mdWrapperQueue = new Queue<Wrapper>();
           
            
                
            foreach (MarketData candle in candles)
            {
                try
                {
                    DoLog($"@{Configuration.Name}--> Publ. Market Data for symbol {candle.Security.Symbol} on date {candle.GetReferenceDateTime()}", Constants.MessageType.Information);

                    Security sec = candle.Security.Clone(candle.Security.Symbol);
                    sec.MarketData = candle.Clone();
                    MarketDataWrapper mdWrapper = new MarketDataWrapper(sec, Configuration);
                    mdWrapperQueue.Enqueue(mdWrapper);
                }
                catch (Exception ex)
                {
                    DoLog($"ERROR Processing market data por security {candle.Security.Symbol} and date {candle.GetDateTime()}:{ex.Message}", Constants.MessageType.Error);
                }
            }


            (new Thread(DoPublishMarketDataAync)).Start(new object[] {  mdWrapperQueue });

        }

        protected void DoProcessMarketDataRequestBulk(MarketDataRequestBulk mdrb)
        {
            try
            {
                DateTime from = GetFrom();
                DateTime to = GetTo();
                List<MarketData> candles = ExtractAllMarketDataBulk(mdrb.Securities, true);

                if (candles.Count > _MIN_CANDLES_FOR_DAY)
                {
                    SetMarketClosingTime(candles);
                    DoPublish(candles);
                }
                else
                {
                    DoLog($"Closing Trading Day because no candles found from={from} to={to} ", Constants.MessageType.Information);
                    TradingBacktestingManager.EndTradingDay();
                }

            }
            catch (Exception ex)
            {

                DoLog($"CRITICAL ERROR Processing market data request bulk :{ex.Message}", Constants.MessageType.Error);
            }
        }

        protected void DoProcessMarketDataRequest(MarketDataRequest mdr)
        {
            try
            {
                DateTime from = GetFrom();
                DateTime to = GetTo();
                List<MarketData> candles = CandleManager.GetCandles(mdr.Security.Symbol, CandleInterval.Minute_1, from, to);

                if (candles.Count > _MIN_CANDLES_FOR_DAY)
                {
                    SetMarketClosingTime(candles);
                    candles = candles.OrderBy(x => x.GetReferenceDateTime()).ToList();
                    DoPublish(candles);
                }
                else
                {
                    DoLog($"Closing Trading Day because no candles found from={from} to={to} ", Constants.MessageType.Information);
                    TradingBacktestingManager.EndTradingDay();
                }

            }
            catch (Exception ex)
            {

                DoLog($"CRITICAL ERROR Processing market data por security {mdr.Security.Symbol} :{ex.Message}", Constants.MessageType.Error);
            }
        }

        protected void EvalSyncBulkWithHistoricalPrices(MarketDataRequestBulk mdrb)
        {

            foreach(Security sec in mdrb.Securities)
            {

                while (!HistoricalPricesFinished(sec.Symbol))
                {
                    DoLog($"Waiting for historical prices to finish to process market data request bulk for symbol {sec.Symbol}", Constants.MessageType.Information);
                    Thread.Sleep(1000);
                }
                double timeToWaitMilisec = Configuration.InitialPacingMarketDataMillisec / mdrb.Securities.Length;
                DoLog($"Waiting {timeToWaitMilisec / 1000} secs to send Market Data for symbol {sec.Symbol}", Constants.MessageType.Information);
                Thread.Sleep(Convert.ToInt32(timeToWaitMilisec));
            }

        }

        protected void EvalSyncWithHistoricalPrices(MarketDataRequest mdr)
        {

            while (!HistoricalPricesFinished(mdr.Security.Symbol))
            {
                DoLog($"Waiting for historical prices to finish to process market data for symbol {mdr.Security.Symbol}", Constants.MessageType.Information);
                Thread.Sleep(1000);
            }
            DoLog($"Waiting {Configuration.InitialPacingMarketDataMillisec / 1000} secs to send Market Data for symbol {mdr.Security.Symbol}", Constants.MessageType.Information);
            Thread.Sleep(Configuration.InitialPacingMarketDataMillisec);
        }

        protected void EvalMarketDataSubscription(string symbol, bool subscrStatus)
        {

            lock (MarketDataUnsubscribeFlagDict)
            {
                if (MarketDataUnsubscribeFlagDict.ContainsKey(symbol))
                    MarketDataUnsubscribeFlagDict[symbol] = subscrStatus;
                else
                    MarketDataUnsubscribeFlagDict.Add(symbol, subscrStatus);
            }
        }

        protected void ProcessMarketDataRequestBulk(object param)
        {
            try
            {

                Wrapper wrapper = (Wrapper)param;
                MarketDataRequestBulk mdrb = MarketDataRequestConverter.GetMarketDataRequestBulk(wrapper);

                EvalSyncBulkWithHistoricalPrices(mdrb);

                if (mdrb.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception($"@{Configuration.Name}: Market Data Request bulk snaphsot not implemented");
                }
                else if (mdrb.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    if (mdrb.MarketDepth == null || mdrb.MarketDepth == MarketDepth.TopOfBook)
                    {
                        mdrb.Securities.ToList().ForEach(x => EvalMarketDataSubscription(x.Symbol, false));
                        DoProcessMarketDataRequestBulk(mdrb);
                    }
                    else if (mdrb.MarketDepth == MarketDepth.FullBook)
                    {
                        throw new Exception($"Market Data Request Bulk --> Full book not implmented @{Configuration.Name}");
                    }
                    else
                    {
                        throw new Exception($"{Configuration.Name}-->Not implemented market depth {mdrb.MarketDepth} on order book request");
                    }

                }
                else if (mdrb.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    mdrb.Securities.ToList().ForEach(x => EvalMarketDataSubscription(x.Symbol, true));
                }
                else
                    throw new Exception($"@{Configuration.Name}: Value not recognized for subscription type {mdrb.SubscriptionRequestType}");

            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL error requesting Market Data: {ex.Message}", Constants.MessageType.Error);
            }



        }

        protected void ProcessMarketDataRequest(object param)
        {
            try
            {

                Wrapper wrapper = (Wrapper)param;
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                EvalSyncWithHistoricalPrices(mdr);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot not implemented for symbol {1}", Configuration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    if (mdr.MarketDepth == null || mdr.MarketDepth == MarketDepth.TopOfBook)
                    {
                        EvalMarketDataSubscription(mdr.Security.Symbol, false);
                        DoProcessMarketDataRequest(mdr);
                    }
                    else if (mdr.MarketDepth == MarketDepth.FullBook)
                    {
                        throw new Exception($"Full book not implmented @{Configuration.Name}");
                    }
                    else
                    {
                        throw new Exception($"{Configuration.Name}-->Not implemented market depth {mdr.MarketDepth} on order book request");
                    }

                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    EvalMarketDataSubscription(mdr.Security.Symbol, true);
                }
                else
                    throw new Exception($"@{Configuration.Name}: Value not recognized for subscription type {mdr.SubscriptionRequestType} for symbol {mdr.Security.Symbol}");

            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL error requesting Market Data: {ex.Message}", Constants.MessageType.Error);
            }

        }

        protected void UpdateHistoricalPricesDict(string symbol, bool finished)
        {
            if (!HistoricalPricesFinishedDict.ContainsKey(symbol))
                HistoricalPricesFinishedDict.Add(symbol, finished);
            else
                HistoricalPricesFinishedDict[symbol] = finished;
        }

        protected void InitializeHistoricalPricesDict(string symbol)
        {

            UpdateHistoricalPricesDict(symbol, false);
        }

        protected void MarkHistoricalPricesFinished(string symbol)
        {
            UpdateHistoricalPricesDict(symbol, true);
        }


        protected bool HistoricalPricesFinished(string symbol)
        {
            if (HistoricalPricesFinishedDict.ContainsKey(symbol))
                return HistoricalPricesFinishedDict[symbol];
            else
                return false;
        }

        

        protected void ProcessHistoricalDataRequestAsync(object param)
        {
            try
            {

                lock (tObject)
                {

                    Wrapper histPrWrapper = (Wrapper)param;

                    HistoricalPricesRequestDTO dto = HistoricalPriceConverter.ConvertHistoricalPriceRequest(histPrWrapper);

                    InitializeHistoricalPricesDict(dto.Symbol);

                    if (!dto.From.HasValue )
                    {
                        throw new Exception($"Historical Prices Request must have From Specified");
                    }
                    else
                    {
                        TimeSpan distance = dto.To.Value - dto.From.Value ;

                        dto.From = GetFrom().AddMinutes(-1 * distance.TotalMinutes);
                        dto.To = GetFrom();
                    
                    }

                    List<MarketData> candles=  CandleManager.GetCandles(dto.Symbol, dto.Interval, dto.From.Value, dto.To.Value);


                    Security mainSec = new Security() { Symbol = dto.Symbol, Currency = dto.Currency, SecType = dto.SecurityType };

                    List<Wrapper> marketDatWrList = new List<Wrapper>();
                    foreach (MarketData candle in candles)
                    {
                        Security sec = new Security() { Symbol = dto.Symbol, Currency = dto.Currency, SecType = dto.SecurityType };
                        sec.MarketData = candle;
                        MarketDataWrapper mdWrp = new MarketDataWrapper(sec, Configuration);
                        marketDatWrList.Add(mdWrp);
                    }

                    HistoricalPricesWrapper histWrp = new HistoricalPricesWrapper(dto.ReqId, mainSec, dto.Interval, marketDatWrList);
                    MarkHistoricalPricesFinished(dto.Symbol);
                    (new Thread(OnPublishAsync)).Start(histWrp);
                }


            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL error requesting Historical Prices : {ex.Message}", Constants.MessageType.Error);
            }
        
        }

        #endregion

        #region Interface/Abstract Methods

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {

                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tObject = new object();
                    HistoricalPricesFinishedDict = new Dictionary<string, bool>();
                    MarketDataUnsubscribeFlagDict = new Dictionary<string, bool>();
                    CandleManager = new CandleManager(Configuration.ConnectionString);

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    Actions action = wrapper.GetAction();
                    if (Actions.MARKET_DATA_REQUEST == action)
                    {
                        string symbol = (string)wrapper.GetField(MarketDataRequestField.Symbol);
                        DoLog($"{Configuration.Name}: Recv Market Data Request for symbol {symbol}", Constants.MessageType.Information);

                        (new Thread(ProcessMarketDataRequest)).Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (Actions.MARKET_DATA_REQUEST_BULK == action)
                    {
                        Security[] securities = (Security[])wrapper.GetField(MarketDataRequestBulkField.Securities);
                        DoLog($"{Configuration.Name}: Recv Market Data Request bulk for securities {securities.Length}", Constants.MessageType.Information);

                        (new Thread(ProcessMarketDataRequestBulk)).Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (Actions.HISTORICAL_PRICES_REQUEST == action)
                    {
                        string symbol = (string)wrapper.GetField(HistoricalPricesRequestFields.Symbol);
                        DoLog($"{Configuration.Name}: Recv Historical Prices Request for symbol {symbol}", Constants.MessageType.Information);
                        (new Thread(ProcessHistoricalDataRequestAsync)).Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else if (Actions.SECURITY_LIST_REQUEST == action)
                    {
                        DoLog($"{Configuration.Name}: Recv Security List Request ", Constants.MessageType.Information);
                        //return ProcessSecurityListRequest(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else
                    {
                        DoLog(string.Format("@{0}:Sending message {1} not implemented", Configuration.Name, action.ToString()), Constants.MessageType.Information);
                        return CMState.BuildFail(new Exception(string.Format("@{0}:Sending message {1} not implemented", Configuration.Name, action.ToString())));
                    }
                }
                else
                    throw new Exception("Invalid Wrapper");


            }
            catch (Exception ex)
            {
                DoLog(ex.Message, Constants.MessageType.Error);
                throw;
            }
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            Configuration = new Configuration().GetConfiguration<Configuration>(configFile, noValueFields);
        }

        protected override IConfiguration GetConfig()
        {
            return Configuration;
        }

        #endregion
    }
}

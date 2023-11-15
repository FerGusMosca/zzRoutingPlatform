using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
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

        protected Dictionary<string,bool> MarketDataUnsubscribeFlagDict { get; set; }

        #endregion


        #region Protected Methods

        protected void DoPublishMarketDataAync(object param)
        {
            try
            {
                object[] paramList = (object[])param;

                Security sec = (Security)paramList[0];
                List<Wrapper> mdWrappperList = (List<Wrapper>)paramList[1];

                foreach(Wrapper md in mdWrappperList)
                {
                    DoLog($"{Configuration.Name}: Publishing Market Data:{md.ToString()}", Constants.MessageType.Information);

                    (new Thread(OnPublishAsync)).Start(md);
                    Thread.Sleep(Configuration.PacingMarketDataMilliSec);
                    if (EvalUnsubscription(sec.Symbol))
                    {
                        DoLog($"Unbubscribing market data for symbol {sec.Symbol}", Constants.MessageType.PriorityInformation);
                        break;
                    }    
                }
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

        protected void DoProcessMarketData(MarketDataRequest mdr)
        {
            try
            {

                List<MarketData>  candles = CandleManager.GetCandles(mdr.Security.Symbol, CandleInterval.Minute_1, Configuration.From, Configuration.To);
                
                Security mainSec = candles.Count > 0 ? candles[0].Security : null;

                List<Wrapper> mdWrapperList = new List<Wrapper>();
                foreach(MarketData candle in candles)
                {
                    try
                    {
                        Security sec = new Security() { Symbol = mainSec.Symbol, SecurityDesc = mainSec.SecurityDesc, SecType = mainSec.SecType, Currency = mainSec.Currency, Exchange = mainSec.Exchange };
                        sec.MarketData = candle;
                        MarketDataWrapper mdWrapper = new MarketDataWrapper(sec, Configuration);
                        mdWrapperList.Add(mdWrapper);
                    }
                    catch (Exception ex)
                    {
                        DoLog($"ERROR Processing market data por security {mainSec.Symbol} and date {candle.GetDateTime()}:{ex.Message}", Constants.MessageType.Error);
                    }
                }


                (new Thread(DoPublishMarketDataAync)).Start(new object[] { mainSec,mdWrapperList });
            }
            catch (Exception ex)
            {

                DoLog($"CRITICAL ERROR Processing market data por security {mdr.Security.Symbol} :{ex.Message}", Constants.MessageType.Error);
            }
        
        
        }

        protected void ProcessMarketDataRequest(object param)
        {

            try
            {
                Wrapper wrapper = (Wrapper)param;
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot not implemented for symbol {1}", Configuration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    if (mdr.MarketDepth == null || mdr.MarketDepth == MarketDepth.TopOfBook)
                    {
                        lock(MarketDataUnsubscribeFlagDict)
                        {
                            MarketDataUnsubscribeFlagDict.Add(mdr.Security.Symbol, false);

                        }
                        DoProcessMarketData(mdr);
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
                    lock (MarketDataUnsubscribeFlagDict)
                    {
                        if (MarketDataUnsubscribeFlagDict.ContainsKey(mdr.Security.Symbol))
                            MarketDataUnsubscribeFlagDict[mdr.Security.Symbol] = true;
                        else
                            MarketDataUnsubscribeFlagDict.Add(mdr.Security.Symbol, true);
                    
                    }
                }
                else
                    throw new Exception($"@{Configuration.Name}: Value not recognized for subscription type {mdr.SubscriptionRequestType} for symbol {mdr.Security.Symbol}");

            }
            catch (Exception ex)
            {
                DoLog($"CRITICAL error requesting Market Data: {ex.Message}", Constants.MessageType.Error);
            }

        }

        protected void ProcessHistoricalDataRequestAsync(object param)
        {
            try
            {

                lock (tObject)
                {

                    Wrapper histPrWrapper = (Wrapper)param;

                    HistoricalPricesRequestDTO dto = HistoricalPriceConverter.ConvertHistoricalPriceRequest(histPrWrapper);

                    if (!dto.From.HasValue && !dto.To.HasValue)
                    {
                        throw new Exception($"Historical Prices Request must have From AND To Specified");
                    }
                    else
                    {
                        TimeSpan distance = dto.To.Value - dto.From.Value ;

                        dto.From = Configuration.From.AddMinutes(-1 * distance.TotalMinutes);
                        dto.To = Configuration.From;
                    
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

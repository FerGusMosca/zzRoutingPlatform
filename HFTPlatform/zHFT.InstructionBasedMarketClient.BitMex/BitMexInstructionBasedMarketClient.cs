using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.FullMrktConnectivity.BitMex.Common.DTO.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.Websockets.Events;
using zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers;
using zHFT.InstructionBasedMarketClient.BitMex.DAL.REST;
using zHFT.InstructionBasedMarketClient.BitMex.DAL.Websockets;
using zHFT.InstructionBasedMarketClient.BitMex.Logic;
using zHFT.InstructionBasedMarketClient.Common.Configuration;
using zHFT.InstructionBasedMarketClient.Cryptos.Client;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Converters;
using zHFT.MarketClient.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.BitMex
{
    public class BitMexInstructionBasedMarketClient : BaseInstructionBasedMarketClient
    {
        #region Protected Attributes

        public  MarketDataManager WSMarketDataManager { get; set; }

        public SecurityListManager SecurityListManager { get; set; }

        protected OrderBookHandler OrderBookHandler { get; set; }


        protected List<zHFT.InstructionBasedMarketClient.BitMex.BE.Security> Securities { get; set; }

        #endregion

        #region Protected Methods

        protected BitMex.Common.Configuration.Configuration BitmexConfiguration
        {
            get { return (BitMex.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        #region Overriden

        protected override BaseConfiguration GetConfig()
        {
            return BitmexConfiguration;
        }
        
        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new BitMex.Common.Configuration.Configuration().GetConfiguration<BitMex.Common.Configuration.Configuration>(configFile, noValueFields);

        }

        protected override int GetSearchForInstrInMiliseconds()
        {
            return 0;
        }

        protected override int GetAccountNumber()
        {
            return 0;
        }

        #endregion

        private  void HandleGenericSubscription(WebSocketResponseMessage WebSocketResponseMessage)
        {
            WebSocketSubscriptionResponse resp = (WebSocketSubscriptionResponse)WebSocketResponseMessage;

            if (resp.success)
                DoLog(string.Format("Successfully subscribed to {0} event on symbol {1}",
                                            resp.GetSubscriptionEvent(), resp.GetSubscriptionAsset()),Main.Common.Util.Constants.MessageType.Information);
            else
                Console.WriteLine(string.Format("Error on subscription to {0} event on symbol {1}:{2}",
                                            resp.GetSubscriptionEvent(), resp.GetSubscriptionAsset(),resp.error), Main.Common.Util.Constants.MessageType.Error);
        }

        protected void OrderBookSubscriptionResponse(WebSocketResponseMessage WebSocketResponseMessage)
        {
            HandleGenericSubscription(WebSocketResponseMessage);
        }

        protected  void TradeSubscriptionResponse(WebSocketResponseMessage subcrResp)
        {
            HandleGenericSubscription(subcrResp);
        }

        //We update an orderBook in memory and publish
        protected  void UpdateOrderBook(WebSocketSubscriptionEvent subscrEvent)
        {
            WebSocketOrderBookL2Event orderBookEvent = (WebSocketOrderBookL2Event)subscrEvent;

            try
            {
                OrderBookEntry[] bids = orderBookEvent.data.Where(x => x.IsBuy()).OrderByDescending(x => x.price).ToArray();
                OrderBookEntry[] asks = orderBookEvent.data.Where(x => x.IsSell()).OrderBy(x => x.price).ToArray();

                OrderBookHandler.DoUpdateOrderBooks(orderBookEvent.action, bids, asks);

                string symbol = "";
                if (bids.Length > 0)
                    symbol = bids[0].symbol;
                else if (asks.Length > 0)
                    symbol = asks[0].symbol;
                else
                    return;

                if (ActiveSecurities.Values.Any(x => x.Symbol == symbol) && OrderBookHandler.OrderBooks.ContainsKey(symbol))
                {
                    Security sec = ActiveSecurities.Values.Where(x => x.Symbol == symbol).FirstOrDefault();

                    OrderBookDictionary dict = OrderBookHandler.OrderBooks[symbol];
                    OrderBookEntry bestBid = dict.Entries.Values.Where(x => x.IsBuy() && x.size > 0).OrderByDescending(x => x.price).FirstOrDefault();
                    OrderBookEntry bestAsk = dict.Entries.Values.Where(x => x.IsSell() && x.size > 0).OrderBy(x => x.price).FirstOrDefault();

                    sec.MarketData.BestBidPrice = Convert.ToDouble(bestBid.price);
                    sec.MarketData.BestBidSize = Convert.ToInt64(bestBid.size);
                    sec.MarketData.BestAskPrice = Convert.ToDouble(bestAsk.price);
                    sec.MarketData.BestAskSize = Convert.ToInt64(bestAsk.size);

                    DoLog(string.Format("@{5}:Publishing Order Book  for symbol {0}: Best Bid Size={1} Best Bid Price={2} Best Ask Size={3} Best Ask Price={4}",
                                        symbol, bestBid.size.ToString("##.########"), bestBid.price.ToString("##.##"),
                                              bestAsk.size.ToString("##.########"), bestAsk.price.ToString("##.##"),
                                        BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);



                    MarketDataWrapper wrapper = new MarketDataWrapper(sec, GetConfig());
                    OnMessageRcv(wrapper);


                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error processing order book :{1}", BitmexConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        protected void UpdateTrade(WebSocketSubscriptionEvent subscrEvent)
        {
            WebSocketTradeEvent trades = (WebSocketTradeEvent)subscrEvent;
            foreach (Trade trade in trades.data.OrderBy(x=>x.timestamp))
            {
                try
                {
                    if (ActiveSecurities.Values.Where(x => x.Symbol == trade.symbol).FirstOrDefault() != null)
                    {
                        Security sec = ActiveSecurities.Values.Where(x => x.Symbol == trade.symbol).FirstOrDefault();

                        sec.MarketData.Trade = Convert.ToDouble(trade.price);
                        sec.MarketData.MDTradeSize = Convert.ToDouble(trade.size);
                        sec.MarketData.LastTradeDateTime = trade.timestamp;

                        DoLog(string.Format("@{5}:NEW TRADE for symbol {0}: Side={1} Size={2} Price={3} TickDirection={4}",
                            trade.symbol, trade.side, trade.size.ToString("##.##"), trade.price.ToString("##.########"), trade.tickDirection,
                            BitmexConfiguration.Name), Main.Common.Util.Constants.MessageType.Information);

                        MarketDataWrapper wrapper = new MarketDataWrapper(sec, GetConfig());
                        OnMessageRcv(wrapper);
                    }
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Error processing trade for symbol {1}:{2}", BitmexConfiguration.Name, trade.symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }

           

        }

        protected override void DoRequestMarketData(Object param)
        {
            string symbol = (string)param;
            try
            {
                DoLog(string.Format("@{0}:Requesting market data por symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                if (ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                {
                    lock (tLock)
                    {
                        WSMarketDataManager.SubscribeOrderBookL2(symbol);
                        WSMarketDataManager.SubscribeTrades(symbol);
                    }
                }
                else
                {
                    DoLog(string.Format("@{0}:Unsubscribing market data for symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}: Error Requesting market data por symbol {1}:{2}", BitmexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        }

        #endregion

        #region Public Methods

        protected override CMState ProcessSecurityListRequest(Wrapper wrapper)
        {
            try
            {
                BitmexSecurityListWrapper secListWrapper = new BitmexSecurityListWrapper(Securities, Config);

                OnMessageRcv(secListWrapper);

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        protected void ProcessContractSize(List<BE.Security> Securities)
        {
            foreach (BE.Security future in Securities.Where(x => x.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.FUT))
            {

                if (future.Symbol.ToUpper() != "XBT")
                {
                    BE.Security bitcoinFuture = Securities.Where(x => x.SecurityType == zHFT.InstructionBasedMarketClient.BitMex.BE.SecurityType.FUT
                                                                && x.Symbol == "XBT" && x.MaturityMonthYear == future.MaturityMonthYear).FirstOrDefault();

                    if (bitcoinFuture != null)
                    {

                        if (!bitcoinFuture.LastTrade.HasValue)
                            throw new Exception(string.Format("Missing Bitcoin future last trade for symbol {0}", bitcoinFuture.Symbol));

                        if (!future.LastTrade.HasValue)
                            throw new Exception(string.Format("Missing  future last trade for symbol {0}", future.Symbol));


                        if (bitcoinFuture.LastTrade.HasValue && future.LastTrade.HasValue)
                        {
                            double cryptoSpotPrice = bitcoinFuture.LastTrade.Value * future.LastTrade.Value;
                            future.LotSize *= Convert.ToDecimal(cryptoSpotPrice);
                        }

                    }
                    else
                        throw new Exception(string.Format("@{0}: Could not process contract size for symbol {1}. Not bitcoin future found for maturity {2}",
                                               Config.Name, future.Symbol, future.MaturityMonthYear));
                }
            
            
            }
        
        }

        protected override CMState ProessMarketDataRequest(Wrapper wrapper)
        {
            try
            {
                MarketDataRequest mdr = MarketDataRequestConverter.GetMarketDataRequest(wrapper);

                if (mdr.SubscriptionRequestType == SubscriptionRequestType.Snapshot)
                {
                    throw new Exception(string.Format("@{0}: Market Data snaphsot not implemented for symbol {1}", BitmexConfiguration.Name, mdr.Security.Symbol));
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.SnapshotAndUpdates)
                {
                    return ProcessMarketDataRequest(wrapper);
                }
                else if (mdr.SubscriptionRequestType == SubscriptionRequestType.Unsuscribe)
                {
                    CancelMarketData(mdr.Security);
                    return CMState.BuildSuccess();
                }
                else
                    throw new Exception(string.Format("@{0}: Value not recognized for subscription type {1} for symbol {2}", BitmexConfiguration.Name, mdr.SubscriptionRequestType.ToString(), mdr.Security.Symbol));
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    WSMarketDataManager = new MarketDataManager(BitmexConfiguration.WebsocketURL, true);

                    SecurityListManager = new DAL.REST.SecurityListManager(BitmexConfiguration.RESTURL);

                    Securities = SecurityListManager.GetActiveSecurityList();

                    ProcessContractSize(Securities);

                    OrderBookHandler = new OrderBookHandler();

                    WSMarketDataManager.SubscribeResponseRequest(
                                                BaseManager._ORDERBOOK_L2,
                                                OrderBookSubscriptionResponse,
                                                new object[] { });


                    WSMarketDataManager.SubscribeResponseRequest(
                                                         BaseManager._TRADE,
                                                         TradeSubscriptionResponse,
                                                         new object[] { });

                    WSMarketDataManager.SubscribeEvents(BaseManager._ORDERBOOK_L2, UpdateOrderBook);
                    WSMarketDataManager.SubscribeEvents(BaseManager._TRADE, UpdateTrade);

                    ActiveSecurities = new Dictionary<int, Security>();
                    ContractsTimeStamps = new Dictionary<int, DateTime>();
                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}

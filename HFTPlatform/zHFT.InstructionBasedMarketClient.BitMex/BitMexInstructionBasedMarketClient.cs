using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BitMex.Common.DTO;
using zHFT.InstructionBasedMarketClient.BitMex.Common.Wrappers;
using zHFT.InstructionBasedMarketClient.BitMex.DAL;
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

namespace zHFT.InstructionBasedMarketClient.BitMex
{
    public class BitMexInstructionBasedMarketClient : BaseInstructionBasedMarketClient
    {
        #region Protected Attributes

        public MarketDataManager MarketDataManager { get; set; }

        #endregion

        #region Protected Methods

        protected BitMex.Common.Configuration.Configuration BitmexConfiguration
        {
            get { return (BitMex.Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

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

        protected override void DoRequestMarketData(Object param)
        {
            string symbol = (string)param;
            try
            {
                DoLog(string.Format("@{0}:Requesting market data por symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);
                int timoutMillisec = 2000;
                
                bool activo = true;
                while (activo)
                {
                    Thread.Sleep(BitmexConfiguration.PublishUpdateInMilliseconds);

                    lock (tLock)
                    {
                        if (ActiveSecurities.Values.Where(x => x.Active).Any(x => x.Symbol == symbol))
                        {
                            try
                            {
                                zHFT.InstructionBasedMarketClient.BitMex.Common.DTO.MarketData md = MarketDataManager.GetMarketData(symbol);
                                Thread.Sleep(timoutMillisec);
                                List<OrderBookEntry> orderBookEntry = MarketDataManager.GetOrderBook(symbol);
                                md.BestBidSize = Convert.ToInt64(OrderBookEntry.GetBestBidSize(orderBookEntry));
                                md.BestAskSize = Convert.ToInt64(OrderBookEntry.GetBestAskSize(orderBookEntry));
                                Thread.Sleep(timoutMillisec);
                                List<Trade> trades =MarketDataManager.GetTrades(symbol, 1);
                                md.LastTradeDate = trades.FirstOrDefault().timestamp;
                                md.LastTradeSize = trades.FirstOrDefault().size;

                                BitmexMarketDataWrapper mdWrapper = new BitmexMarketDataWrapper(md);
                                OnMessageRcv(mdWrapper);
                            }
                            catch (Exception ex)
                            {
                                DoLog(string.Format("@{0}:Error Requesting market data for symbol {1}:{2}",
                                        BitmexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Information);
                                //activo = false;
                            }
                        }
                        else
                        {
                            DoLog(string.Format("@{0}:Unsubscribing market data for symbol {1}", BitmexConfiguration.Name, symbol), Main.Common.Util.Constants.MessageType.Information);

                            activo = false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                lock (tLock)
                {
                    RemoveSymbol(symbol);
                }

                DoLog(string.Format("@{0}: Error Requesting market data por symbol {1}:{2}", BitmexConfiguration.Name, symbol, ex.Message), Main.Common.Util.Constants.MessageType.Error);
            }
        
        }


        #endregion

        #region Public Methods

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
                    MarketDataManager = new MarketDataManager(BitmexConfiguration.URL);
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

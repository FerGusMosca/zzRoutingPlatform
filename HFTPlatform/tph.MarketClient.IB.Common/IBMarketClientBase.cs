using IBApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.InstructionBasedMarketClient.IB.Common.Converters;
using tph.InstructionBasedMarketClient.IB.Common.DTO;
using zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.MarketClient.IB.Common.Configuration;
using zHFT.MarketClient.IB.Common.Converters;
using zHFT.StrategyHandler.Common.Wrappers;
using Constants = zHFT.Main.Common.Util.Constants;
using Contract = IBApi.Contract;
using MarketData = zHFT.Main.BusinessEntities.Market_Data.MarketData;

namespace tph.MarketClient.IB.Common
{
    public abstract class IBMarketClientBase : MarketClientBase, ICommunicationModule, EWrapper, EReaderSignal
    {
        #region Private Consts

        private static string _US_PRIMARY_EXCHANGE = "ISLAND";

        #endregion

        #region Private And Protected Attributes

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected Dictionary<int, Security> ContractRequests { get; set; }

        protected EClientSocket ClientSocket { get; set; }

        protected EReader EReader { get; set; }

        protected EReaderSignal EReaderSignal { get; set; }

        protected Thread ReaderThread { get; set; }

        protected Dictionary<int, HistoricalPricesHoldingDTO> HistoricalPricesRequest { get; set; }

        protected Dictionary<int, Contract> OptionChainRequested { get; set; }
        protected object tLockHistoricalPricesRequest { get; set; }


        #endregion

        #region Public Abstract Mehtods

        public abstract CMState ProcessMessage(Wrapper wrapper);

        public abstract bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile);

        protected abstract void ProcessField(string ev, int tickerId, int field, double value);

        protected abstract void ProcessField(string ev, int tickerId, int field, int value);

        protected abstract void ProcessField(string ev, int tickerId, int field, string value);
        #endregion

        #region Protected Methods

        protected void ReaderThreadImp()
        {
            try
            {
                while (ClientSocket.IsConnected())
                {
                    waitForSignal();
                    EReader.processMsgs();

                }
            }
            catch (Exception e)
            {
                DoLog(string.Format("CRITICAL error processing ReaderThreadImp:{0}", e.Message), Constants.MessageType.Error);
            }
        }

        protected Security BuildSecurityFromConfig(zHFT.MarketClient.IB.Common.Configuration.Contract ctr)
        {
            Security sec = new Security()
            {
                Symbol = ctr.Symbol,
                Exchange = ctr.Exchange,
                Currency = ctr.Currency,
                SecType = zHFT.MarketClient.IB.Common.Converters.SecurityConverter.GetSecurityTypeFromIBCode(ctr.SecType)
            };

            return sec;

        }

        protected void DoRunPublishSecurity(Object param)
        {
            Security sec = (Security)param;
            RunPublishSecurity(sec);
        }

        protected void RunPublishSecurity(Security sec)
        {
            try
            {

                MarketDataWrapper wrapper = new MarketDataWrapper(sec, GetConfig());
                CMState state = OnMessageRcv(wrapper);

                if (state.Success)
                    DoLog(string.Format("IB Publishing Market Data for Security {0} ", sec.Symbol), Constants.MessageType.Information);
                else
                    DoLog(string.Format("Error Publishing Market Data for Security {0}. Error={1} ",
                                        sec.Symbol,
                                        state.Exception != null ? state.Exception.Message : ""),
                                        Constants.MessageType.Error);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error Publishing Market Data for Security {0}. Error={1} ",
                                            sec.Symbol, ex != null ? ex.Message : ""),
                                            Constants.MessageType.Error);
            }

        }

        #endregion

        #region IB Methods

        protected void ReqMktData(int reqId, bool snapshot, zHFT.MarketClient.IB.Common.Configuration.Contract ctr)
        {
            Contract ibContract = new Contract();

            ibContract.Symbol = ctr.Symbol;
            ibContract.SecType = ctr.SecType;
            ibContract.Exchange = ctr.Exchange;
            ibContract.Currency = ctr.Currency;
            ibContract.PrimaryExch = ctr.PrimaryExchange;

            //ClientSocket.reqMktData(reqId, ibContract, "", snapshot, null);

            ClientSocket.reqMktData(reqId, ibContract, "", snapshot, false, new List<TagValue>());
        }

        protected void ReqMarketDepth(int reqId, zHFT.MarketClient.IB.Common.Configuration.Contract ctr)
        {
            Contract ibContract = new Contract();

            ibContract.Symbol = ctr.Symbol;
            ibContract.SecType = ctr.SecType;
            ibContract.Exchange = ctr.Exchange;
            ibContract.Currency = ctr.Currency;
            ibContract.PrimaryExch = ctr.PrimaryExchange;

            ClientSocket.reqMarketDepth(reqId, ibContract, 5, new List<TagValue>());
        }

        public void accountDownloadEnd(string account)
        {
            DoLog(string.Format("accountDownloadEnd: account={0}",
                                account), Constants.MessageType.Information);
        }

        public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId,
            int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            DoLog(string.Format("orderStatus: orderId={0} status={1} filled={2} remaining={3} avgFillPrice={4} permId={5} parentId={6} lastFillPrice={7} clientId={8} whyHeld={9}  ",
                orderId,
                status,
                filled,
                remaining,
                avgFillPrice,
                permId,
                parentId,
                lastFillPrice,
                clientId,
                whyHeld), Constants.MessageType.Information);
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            DoLog(string.Format("accountSummary: reqId={0} account={1} tag={2} value={3} currency={4}  ",
                                reqId,
                                account,
                                tag,
                                value,
                                currency), Constants.MessageType.Information);
        }

        public void accountSummaryEnd(int reqId)
        {
            DoLog(string.Format("accountSummaryEnd: reqId={0}",
                                reqId), Constants.MessageType.Information);
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            DoLog(string.Format("bondContractDetails: reqId={0} contractDetails={1}",
                                reqId,
                                contract.ToString()), Constants.MessageType.Information);
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            DoLog(string.Format("commissionReport: commissionReport={0}",
                                commissionReport.ToString()), Constants.MessageType.Information);
        }

        public void connectionClosed()
        {
            DoLog(string.Format("connectionClosed"), Constants.MessageType.Information);
        }


        private void EvalOptionChainRequest(int reqId, ContractDetails contractDetails)
        {

            try
            {
                lock (OptionChainRequested)
                {
                    if (OptionChainRequested.ContainsKey(reqId))
                    {
                        DoLog($"Recv Contract Details for symbol {contractDetails.UnderSymbol} (ReqId={reqId})", Constants.MessageType.Information);

                        Contract underContract = OptionChainRequested[reqId];

                        int contractId = 0;

                        if (contractDetails.Summary != null)
                            contractId = contractDetails.Summary.ConId;
                        else
                            throw new Exception($"CondId value could not be found for security {contractDetails.UnderSymbol}");

                        DoLog($"Extracting ContractId -->{contractId}", Constants.MessageType.Information);
                        ClientSocket.reqSecDefOptParams(reqId, underContract.Symbol, null, underContract.SecType, contractId);
                    }
                }
            }
            catch (Exception ex) {
                DoLog($"CRITICAL ERROR processing contract details for symbol {contractDetails.UnderSymbol}", Constants.MessageType.Error);
            }

        }

        public virtual void contractDetails(int reqId, ContractDetails contractDetails)
        {


            DoLog(string.Format("contractDetails: reqId={0} contractDetails={1}",
                                reqId,
                                contractDetails.ToString()), Constants.MessageType.Information);


            EvalOptionChainRequest(reqId, contractDetails);

        }

        public virtual void contractDetailsEnd(int reqId)
        {
            DoLog(string.Format("contractDetailsEnd: reqId={0}",
                                reqId), Constants.MessageType.Information);
        }

        public void currentTime(long time)
        {
            DoLog(string.Format("currentTime: time={0}",
                                time), Constants.MessageType.Information);
        }

        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            ProcessField("tickPrice", tickerId, field, price);
        }

        public void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            DoLog(string.Format("deltaNeutralValidation: reqId={0} groups={1}",
                                reqId,
                                underComp.ToString()), Constants.MessageType.Information);
        }

        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            DoLog(string.Format("error: isSuccessful={0} errorText={1}",
                isSuccessful,
                errorText), Constants.MessageType.Information);
        }

        public void displayGroupList(int reqId, string groups)
        {
            DoLog(string.Format("error: reqId={0} groups={1}",
                                reqId,
                                groups), Constants.MessageType.Information);
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            DoLog(string.Format("error: reqId={0} contractInfo={1}",
                                reqId,
                                contractInfo), Constants.MessageType.Information);
        }

        public void connectAck()
        {
            DoLog(string.Format("connectAck"), Constants.MessageType.Information);
        }

        public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
            DoLog(string.Format("positionMulti: requestId={0} account={1} modelCode={2} contract={3} pos={4} avgCost={5}",
                requestId,
                account,
                modelCode,
                contract.Symbol,
                pos,
                avgCost), Constants.MessageType.Information);
        }

        public void positionMultiEnd(int requestId)
        {
            DoLog(string.Format("positionMultiEnd: requestId={0}", requestId), Constants.MessageType.Information);
        }

        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            DoLog(string.Format("accountUpdateMulti: requestId={0} account={1} modelCode={2} key={3} value={4} currency={5}",
                requestId,
                account,
                modelCode,
                key,
                value,
                currency), Constants.MessageType.Information);
        }

        public void accountUpdateMultiEnd(int requestId)
        {
            DoLog(string.Format("accountUpdateMultiEnd: requestId={0} ", requestId), Constants.MessageType.Information);
        }

        private void EvalOptionChainResponse(int reqId, string exchange, int underlyingConId, string tradingClass,
            string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            try
            {
                lock (OptionChainRequested)
                {
                    if (OptionChainRequested.ContainsKey(reqId))
                    {
                        Contract contract = OptionChainRequested[reqId];

                        if (contract.Exchange == exchange)
                        {

                            List<Security> optionChain = zHFT.MarketClient.IB.Common.Converters.SecurityConverter.BuildOptionChainSecurities(reqId, contract.Symbol, contract.Currency,
                                                                                                      exchange, underlyingConId, tradingClass,
                                                                                                      multiplier, expirations, strikes);

                            List<Wrapper> secWrappers = new List<Wrapper>();
                            foreach (Security option in optionChain)
                            {
                                SecurityWrapper wrapper = new SecurityWrapper(option, null);
                                secWrappers.Add(wrapper);

                            }


                            SecurityListWrapper wrapperList = new SecurityListWrapper(reqId, secWrappers, zHFT.Main.Common.Enums.SecurityListRequestType.OptionChain, "IB");

                            (new Thread(OnPublishAsync)).Start(wrapperList);
                        }
                        else
                        {
                            DoLog($"Ignoring contracts for not wanted exchange {exchange}", Constants.MessageType.Debug);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                DoLog($"ERROR evaluating option chain response for reqId {reqId}:{ex.Message}", Constants.MessageType.Error);
            
            }
        }

        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass,
            string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            DoLog(string.Format("securityDefinitionOptionParameter: reqId={0} exchange={1} underlyingConId={2} tradingClass={3} multiplier={4} " +
                                    "expirationsCount={5} strikesCount={6}",
                                    reqId,
                                    exchange,
                                    underlyingConId,
                                    tradingClass,
                                    multiplier,
                                    expirations.Count,
                                    strikes.Count), Constants.MessageType.Information);
            EvalOptionChainResponse(reqId,exchange,underlyingConId,tradingClass,multiplier,expirations, strikes);
        }

        public void securityDefinitionOptionParameterEnd(int reqId)
        {
            DoLog(string.Format("securityDefinitionOptionParameterEnd: reqId={0} ",reqId), Constants.MessageType.Information);
        }

        public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            DoLog(string.Format("softDollarTiers: reqId={0} tierLength={1}",
                reqId,
                tiers.Length), Constants.MessageType.Information);
        }

        public void familyCodes(FamilyCode[] familyCodes)
        {
            DoLog(string.Format("familyCodes: reqId={0} familyCodesLength={1}",
                0,
                familyCodes.Length), Constants.MessageType.Information);
        }

        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            DoLog(string.Format("symbolSamples: reqId={0} contractDescriptionsLength={1}",
                reqId,
                contractDescriptions.Length), Constants.MessageType.Information);
        }

        public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            DoLog(string.Format("mktDepthExchanges: reqId={0} depthMktDataDescriptionsLength={1}",
                0,
                depthMktDataDescriptions.Length), Constants.MessageType.Information);
        }

        public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            DoLog(string.Format("tickNews: reqId={0} timeStamp={1} providerCode={2} articleId={3} headline={4} extraData={5}",
                tickerId,timeStamp,providerCode,articleId,headline,extraData), Constants.MessageType.Information);
        }

        public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            DoLog(string.Format("smartComponents: reqId={0} theMapLength={1}",reqId,theMap.Count), Constants.MessageType.Information);
        }

        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            DoLog(string.Format("tickReqParams: tickerId={0} minTick={1}  bboExchange={2}  snapshotPermissions={3}",tickerId,minTick,bboExchange,snapshotPermissions), 
                Constants.MessageType.Information);
        }

        public void newsProviders(NewsProvider[] newsProviders)
        {
            DoLog(string.Format("newsProviders: tickerId={0} newsProvidersLength={1}", 0, newsProviders.Length),
                Constants.MessageType.Information);
        }

        public void newsArticle(int requestId, int articleType, string articleText)
        {
            DoLog(string.Format("newsArticle: tickerId={0} articleType={1} articleText={2}", requestId, articleType,articleText),Constants.MessageType.Information);
        }

        public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            DoLog(string.Format("historicalNews: tickerId={0} articleType={1} articleText={2}  articleText={3}  articleText={4}", 
                                    requestId, time,providerCode,articleId,headline),Constants.MessageType.Information);
        }

        public void historicalNewsEnd(int requestId, bool hasMore)
        {
            DoLog(string.Format("historicalNewsEnd: tickerId={0} hasMore={1} ", requestId, hasMore),Constants.MessageType.Information);
        }

        public void headTimestamp(int reqId, string headTimestamp)
        {
            DoLog(string.Format("headTimestamp: reqId={0} headTimestamp={1} ", reqId, headTimestamp),Constants.MessageType.Information);
        }

        public void histogramData(int reqId, HistogramEntry[] data)
        {
            DoLog(string.Format("histogramData: reqId={0} dataLength={1} ", reqId, data.Length),Constants.MessageType.Information);
        }

        public void rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            DoLog(string.Format("rerouteMktDataReq: reqId={0} conId={1} exchange={2} ", reqId, conId,exchange),Constants.MessageType.Information);
        }

        public void rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            DoLog(string.Format("rerouteMktDepthReq: reqId={0} conId={1} exchange={2} ", reqId, conId,exchange),Constants.MessageType.Information);
        }

        public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            DoLog(string.Format("marketRule: marketRuleId={0} marketRuleIdLength={1} ", marketRuleId, priceIncrements.Length),Constants.MessageType.Information);
        }

        public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            DoLog(string.Format("pnl: reqId={0} dailyPnL={1}  unrealizedPnL={2}  realizedPnL={3} ",
                reqId, dailyPnL, unrealizedPnL, realizedPnL),Constants.MessageType.Information);
        }

        public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
        {
            DoLog(string.Format("pnlSingle: reqId={0} pos={1}  dailyPnL={2}  unrealizedPnL={3}  realizedPnL={4}  value={5} ",
                reqId, pos, dailyPnL, unrealizedPnL, realizedPnL,value),Constants.MessageType.Information);
        }

        public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
        {
            DoLog(string.Format("historicalTicks: reqId={0} ticksLength={1}  done={2}  ",
                reqId, ticks.Length, done),Constants.MessageType.Information);
        }

        public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
        {
            DoLog(string.Format("historicalTicksBidAsk: reqId={0} ticksLength={1}  done={2}  ",
                reqId, ticks.Length, done),Constants.MessageType.Information);
        }

        public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
        {
            DoLog(string.Format("historicalTicksLast: reqId={0} ticksLength={1}  done={2}  ",
                reqId, ticks.Length, done),Constants.MessageType.Information);
        }

        public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttrib attribs, string exchange,
            string specialConditions)
        {
            DoLog(string.Format("tickByTickAllLast: reqId={0} tickType={1}  time={2}  price={3}  size={4}  exchange={5} specialConditions={6} ",
                reqId, tickType, time, price, size,exchange,specialConditions),Constants.MessageType.Information);
        }

        public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize,
            TickAttrib attribs)
        {
            DoLog(string.Format(
                    "tickByTickBidAsk: reqId={0} time={1}  bidPrice={2}  askPrice={3}  bidSize={4}  askSize={5} ",
                    reqId, time, bidPrice, askPrice, bidSize, askSize),Constants.MessageType.Information);
        }

        public void tickByTickMidPoint(int reqId, long time, double midPoint)
        {
            DoLog(string.Format("tickByTickMidPoint: reqId={0} time={1}  midPoint={2} ",reqId, time, midPoint),
                Constants.MessageType.Information);
        }

        public void error(int id, int errorCode, string errorMsg)
        {
            DoLog(string.Format("error: id={0} errorCode={1} errorMsg={2}  ",
                                id,
                                errorCode,
                                errorMsg), Constants.MessageType.Information);
        }

        public void error(string str)
        {
            DoLog(string.Format("error: str={0}   ",
                                str), Constants.MessageType.Information);
        }

        public void error(Exception e)
        {
            DoLog(string.Format("error: ex={0}   ",
                                e.Message), Constants.MessageType.Information);
        }

        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            DoLog(string.Format("execDetails: reqId={0} start={1} end={2}  ",
                                reqId,
                                contract.ToString(),
                                execution.ToString()), Constants.MessageType.Information);
        }

        public void execDetailsEnd(int reqId)
        {
            DoLog(string.Format("execDetailsEnd: reqId={0}   ",
                                reqId), Constants.MessageType.Information);
        }

        public void fundamentalData(int reqId, string data)
        {
            DoLog(string.Format("fundamentalData: reqId={0} data={1}  ",
                                reqId,
                                data), Constants.MessageType.Information);
        }

        public void historicalData(int reqId, Bar bar)
        {
            lock (HistoricalPricesRequest)
            {
                try
                {
                    if (HistoricalPricesRequest.ContainsKey(reqId))
                    {
                        HistoricalPricesHoldingDTO dtoRecord = HistoricalPricesRequest[reqId];
                        zHFT.Main.BusinessEntities.Market_Data.MarketData md =new zHFT.Main.BusinessEntities.Market_Data.MarketData();

                        DateTime date = IBDateTimeConverter.ConvertBarDateTime(bar.Time);

                        md.MDEntryDate = date;
                        md.MDLocalEntryDate = date;
                        md.Trade = bar.Close;
                        md.OpeningPrice = bar.Open;
                        md.ClosingPrice = bar.Close;
                        md.TradingSessionHighPrice = bar.High;
                        md.TradingSessionLowPrice = bar.Low;
                        md.CashVolume = bar.Volume;
                        md.NominalVolume = bar.Count; //number of trades
                        //bar.WAP;

                        dtoRecord.MarketDataList.Add(md);

                        DoLog(string.Format(
                            "historicalData: reqId={0} date={1} open={2} high={3} low={4} close={5} close={6} volume={7} count={8} WAP={9} ",
                            reqId,bar.Time,bar.Open,bar.High,bar.Low,bar.Close,bar.Close,bar.Volume,bar.Count,bar.WAP), Constants.MessageType.Information);
                    }
                    else
                    {
                        DoLog($"WARNING-Ignoring historical prices for request {reqId}",Constants.MessageType.Information);
                    }
                }
                catch (Exception ex)
                {
                    DoLog($"CRITICAL ERROR Processing historical price for req Id {reqId}: {ex.Message}",Constants.MessageType.Error);
                }
            }
        }

        public void historicalDataUpdate(int reqId, Bar bar)
        {
            DoLog(string.Format("historicalData: reqId={0} date={1} open={2} high={3} low={4} close={5} close={6} volume={7} count={8} WAP={9} ",
                reqId,
                bar.Time,
                bar.Open,
                bar.High,
                bar.Low,
                bar.Close,
                bar.Close,
                bar.Volume,
                bar.Count,
                bar.WAP), Constants.MessageType.Information);
        }

        public void OnPublishAsync(object param)
        {
            try
            {
                CMState state = OnMessageRcv((Wrapper) param);
                if (!state.Success)
                    throw state.Exception;
            }
            catch (Exception e)
            {
                DoLog($"CRITICAL ERROR @OnPublishHistoricalPricesAsync: {e.Message}",Constants.MessageType.Information);
            }
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            lock (HistoricalPricesRequest)
            {
                try
                {
                    if (HistoricalPricesRequest.ContainsKey(reqId))
                    {
                        DoLog(string.Format("historicalDataEnd: reqId={0} start={1} end={2}  ",
                            reqId,
                            start,
                            end), Constants.MessageType.Information);
                        
                        HistoricalPricesHoldingDTO dtoRecord = HistoricalPricesRequest[reqId];
                        
                        Security mainSec = new Security();
                        mainSec.Symbol = dtoRecord.Security.Symbol;
                        mainSec.SecType = dtoRecord.Security.SecType;
                        mainSec.Currency = dtoRecord.Security.Currency;

                        List<Wrapper> marketDataWrapper =  new List<Wrapper>();
                        foreach (zHFT.Main.BusinessEntities.Market_Data.MarketData md in dtoRecord.MarketDataList)
                        {
                            
                            Security sec = new Security();
                            sec.Symbol = dtoRecord.Security.Symbol;
                            sec.SecType = dtoRecord.Security.SecType;
                            sec.Currency = dtoRecord.Security.Currency;
                            sec.MarketData = md;
                            MarketDataWrapper mdWrapper = new MarketDataWrapper(sec, GetConfig());
                            marketDataWrapper.Add(mdWrapper);
                        }


                        HistoricalPricesWrapper histWrp = new HistoricalPricesWrapper(reqId,mainSec, dtoRecord.Interval, marketDataWrapper);
                        HistoricalPricesRequest.Remove(reqId);
                        (new Thread(OnPublishAsync)).Start(histWrp);
                    }
                }
                catch (Exception ex)
                {
                    DoLog($"CRITICAL ERROR Publishing Historical prices: {ex.Message}",Constants.MessageType.Information);
                }
                
            }
        }

        public void managedAccounts(string accountsList)
        {
            DoLog(string.Format("managedAccounts: accountsList={0}",
                                accountsList), Constants.MessageType.Information);
        }

        public void marketDataType(int reqId, int marketDataType)
        {
            DoLog(string.Format("marketDataType: reqId={0} marketDataType={1} ",
                                reqId,
                                marketDataType
                                ), Constants.MessageType.Information);
        }

        public void nextValidId(int orderId)
        {
            DoLog(string.Format("nextValidId: orderId={0}",
                                orderId), Constants.MessageType.Information);
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            DoLog(string.Format("position: orderId={0} contract={1} order={2} orderState={3} ",
                                orderId,
                                contract.ToString(),
                                order.ToString(),
                                orderState.ToString()), Constants.MessageType.Information);
        }

        public void openOrderEnd()
        {
            DoLog(string.Format("openOrderEnd "), Constants.MessageType.Information);
        }

        public void position(string account, Contract contract, double pos, double avgCost)
        {
            DoLog(string.Format("position: account={0} contract={1} pos={2} avgCost={3} ",
                account,
                contract.Symbol.ToString(),
                pos,
                avgCost), Constants.MessageType.Information);
        }

        public void positionEnd()
        {
            DoLog(string.Format("positionEnd "), Constants.MessageType.Information);
        }

        public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            DoLog(string.Format("realtimeBar: reqId={0} time={1} open={2} high={3} low={4} close={5} volume={6} WAP={7} count={8}  ",
                                reqId,
                                time,
                                open,
                                high,
                                low,
                                close,
                                volume,
                                WAP,
                                count), Constants.MessageType.Information);
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            DoLog(string.Format("receiveFA: faDataType={0} faXmlData={1}  ",
                                 faDataType,
                                 faXmlData), Constants.MessageType.Information);
        }

        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            DoLog(string.Format("scannerData: reqId={0} rank={1} contractDetails={2} distance={3} benchmark={4} projection={5} legsStr={6}  ",
                                reqId,
                                rank,
                                contractDetails.ToString(),
                                distance,
                                benchmark,
                                projection,
                                legsStr), Constants.MessageType.Information);
        }

        public void scannerDataEnd(int reqId)
        {
            DoLog(string.Format("scannerDataEnd: reqId={0}  ",
                                reqId), Constants.MessageType.Information);
        }

        public void scannerParameters(string xml)
        {
            DoLog(string.Format("scannerParameters: xml={0}  ",
                                 xml), Constants.MessageType.Information);
        }

        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureExpiry, double dividendImpact, double dividendsToExpiry)
        {
            DoLog(string.Format("tickEFP: tickerId={0} tickType={1} basisPoints={2} formattedBasisPoints={3} impliedFuture={4} holdDays={5} futureExpiry={6} dividendImpact={7} dividendsToExpiry={8} ",
                                tickerId,
                                tickType,
                                basisPoints,
                                formattedBasisPoints,
                                impliedFuture,
                                holdDays,
                                futureExpiry,
                                dividendImpact,
                                dividendsToExpiry), Constants.MessageType.Information);
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            
            DoLog(string.Format("tickGeneric: tickerId={0} field={1} size={2} ",
                                 tickerId,
                                 field,
                                 value), Constants.MessageType.Information);
            ProcessField("tickGeneric", tickerId, field, value);
        }

        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            DoLog(string.Format("tickrequeComputation: tickerId={0} field={1} impliedVolatility={2} delta={3} optPrice={4} pvDividend={5} gamma={6} vega={7} theta={8} undPrice={9} ",
                                tickerId,
                                field,
                                impliedVolatility,
                                delta,
                                optPrice,
                                pvDividend,
                                gamma,
                                vega,
                                theta,
                                undPrice), Constants.MessageType.Information);
        }

        public void tickSize(int tickerId, int field, int size)
        {
            
            //DoLog(string.Format("tickSize: tickerId={0} field={1} size={2} ",
            //                     tickerId,
            //                     TickType.getField(field),
            //                     size), MessageType.Information);
            ProcessField("tickSize", tickerId, field, size);
            
        }

        public virtual void tickSnapshotEnd(int tickerId)
        {
            DoLog(string.Format("tickSnapshotEnd: tickerId={0}",
                                 tickerId), Constants.MessageType.Information);
        }

        public void tickString(int tickerId, int field, string value)
        {
            //DoLog(string.Format("tickString: tickerId={0} field={1} value={2} ",
            //                    tickerId,
            //                    TickType.getField(field),
            //                    value), MessageType.Information);
            ProcessField("tickGeneric", tickerId, field, value);
        }

        public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost,
            double unrealizedPNL, double realizedPNL, string accountName)
        {
            
            DoLog(string.Format("updatePortfolio: contract={0} marketValue={1} position={2} marketPrice={3} averageCost={4}" +
                                        " unrealizedPNL={5} realizedPNL={6} accountName={7} marketValue={8}",
                                    contract.ToString(), marketValue,position,marketPrice,averageCost,unrealizedPNL,realizedPNL,accountName,marketValue),
                                    Constants.MessageType.Information);

        }

        public void updateAccountTime(string timestamp)
        {
            DoLog(string.Format("updateAccountTime: timestamp={0}",
                                 timestamp), Constants.MessageType.Information);
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            DoLog(string.Format("updateAccountValue: key={0} value={1} currency={2} accountName={3}",
                                key,
                                value,
                                currency,
                                accountName), Constants.MessageType.Information);
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            DoLog(string.Format("updateMktDepth: position={0} marketMaker={1} operation={2} side={3} price={4} size={5}",
                                position,
                                "",
                                operation,
                                side,
                                price,
                                size), Constants.MessageType.Information);
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            DoLog(string.Format("updateMktDepthL2: position={0} marketMaker={1} operation={2} side={3} price={4} size={5}",
                                position,
                                marketMaker,
                                operation,
                                side,
                                price,
                                size), Constants.MessageType.Information);

        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            DoLog(string.Format("updateNewsBulletin: msgType={0} message={1} origExchange={2}", msgType, message, origExchange), Constants.MessageType.Information);

        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            DoLog(string.Format("verifyCompleted: isSuccessfull={0} errorText={1}", isSuccessful, errorText), Constants.MessageType.Information);
        }

        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            DoLog(string.Format("verifyAndAuthMessageAPI: apiData={0} xyzChallenge={1}", apiData, xyzChallenge), Constants.MessageType.Information);
        }

        public void verifyMessageAPI(string apiData)
        {
            DoLog("verifyMessageAPI: " + apiData, Constants.MessageType.Information);
        }
        
        public void issueSignal()
        {
            //DoLog("issueSignal" , Constants.MessageType.Information);
        }

        public void waitForSignal()
        {
           //DoLog("waitForSignal" , Constants.MessageType.Information);
        }

        #endregion

       
    }
}

using IBApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.MarketClient.IB.Common.Converters;

namespace zHFT.MarketClient.IB.Common
{
    public abstract class IBMarketClientBase : MarketClientBase, ICommunicationModule, EWrapper
    {
        #region Private Consts

        private static string _US_PRIMARY_EXCHANGE = "ISLAND";

        #endregion

        #region Private And Protected Attributes

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected Dictionary<int, Security> ContractRequests { get; set; }

        protected EClientSocket ClientSocket { get; set; }

        #endregion

        #region Public Abstract Mehtods

        public abstract CMState ProcessMessage(Wrapper wrapper);

        public abstract bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile);

        protected abstract void ProcessField(string ev, int tickerId, int field, double value);

        protected abstract void ProcessField(string ev, int tickerId, int field, int value);

        protected abstract void ProcessField(string ev, int tickerId, int field, string value);
        #endregion

        #region Protected Methods

        protected Security BuildSecurityFromConfig(zHFT.MarketClient.IB.Common.Configuration.Contract ctr)
        {
            Security sec = new Security()
            {
                Symbol = ctr.Symbol,
                Exchange = ctr.Exchange,
                Currency = ctr.Currency,
                SecType = SecurityConverter.GetSecurityTypeFromIBCode(ctr.SecType)
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
                    DoLog(string.Format("IB Publishing Market Data for Security {0} ", sec.Symbol), Main.Common.Util.Constants.MessageType.Information);
                else
                    DoLog(string.Format("Error Publishing Market Data for Security {0}. Error={1} ",
                                        sec.Symbol,
                                        state.Exception != null ? state.Exception.Message : ""),
                                        Main.Common.Util.Constants.MessageType.Error);

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error Publishing Market Data for Security {0}. Error={1} ",
                                            sec.Symbol, ex != null ? ex.Message : ""),
                                            Main.Common.Util.Constants.MessageType.Error);
            }
        
        }

        #endregion

        #region IB Methods

        protected void ReqMktData(int reqId,bool snapshot, zHFT.MarketClient.IB.Common.Configuration.Contract ctr)
        {
            Contract ibContract = new Contract();

            ibContract.Symbol = ctr.Symbol;
            ibContract.SecType = ctr.SecType;
            ibContract.Exchange = ctr.Exchange;
            ibContract.Currency = ctr.Currency;
            ibContract.PrimaryExch = _US_PRIMARY_EXCHANGE;

            ClientSocket.reqMktData(reqId, ibContract, "", snapshot, null);

        }

        protected void ReqMarketDepth(int reqId, zHFT.MarketClient.IB.Common.Configuration.Contract ctr)
        {
            Contract ibContract = new Contract();

            ibContract.Symbol = ctr.Symbol;
            ibContract.SecType = ctr.SecType;
            ibContract.Exchange = ctr.Exchange;
            ibContract.Currency = ctr.Currency;
            ibContract.PrimaryExch = _US_PRIMARY_EXCHANGE;

            ClientSocket.reqMarketDepth(reqId, ibContract, 5, null);

        }
       
        public void accountDownloadEnd(string account)
        {
            DoLog(string.Format("accountDownloadEnd: account={0}",
                                account), Main.Common.Util.Constants.MessageType.Information);
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            DoLog(string.Format("accountSummary: reqId={0} account={1} tag={2} value={3} currency={4}  ",
                                reqId,
                                account,
                                tag,
                                value,
                                currency), Main.Common.Util.Constants.MessageType.Information);
        }

        public void accountSummaryEnd(int reqId)
        {
            DoLog(string.Format("accountSummaryEnd: reqId={0}",
                                reqId), Main.Common.Util.Constants.MessageType.Information);
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            DoLog(string.Format("bondContractDetails: reqId={0} contractDetails={1}",
                                reqId,
                                contract.ToString()), Main.Common.Util.Constants.MessageType.Information);
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            DoLog(string.Format("commissionReport: commissionReport={0}",
                                commissionReport.ToString()), Main.Common.Util.Constants.MessageType.Information);
        }

        public void connectionClosed()
        {
            DoLog(string.Format("connectionClosed"), Main.Common.Util.Constants.MessageType.Information);
        }

        public virtual void contractDetails(int reqId, ContractDetails contractDetails)
        {
            DoLog(string.Format("contractDetails: reqId={0} contractDetails={1}",
                                reqId,
                                contractDetails.ToString()), Main.Common.Util.Constants.MessageType.Information);
        }

        public virtual void contractDetailsEnd(int reqId)
        {
            DoLog(string.Format("contractDetailsEnd: reqId={0}",
                                reqId), Main.Common.Util.Constants.MessageType.Information);
        }

        public void currentTime(long time)
        {
            DoLog(string.Format("currentTime: time={0}",
                                time), Main.Common.Util.Constants.MessageType.Information);
        }

        public void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            DoLog(string.Format("deltaNeutralValidation: reqId={0} groups={1}",
                                reqId,
                                underComp.ToString()), Main.Common.Util.Constants.MessageType.Information);
        }

        public void displayGroupList(int reqId, string groups)
        {
            DoLog(string.Format("error: reqId={0} groups={1}",
                                reqId,
                                groups), Main.Common.Util.Constants.MessageType.Information);
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            DoLog(string.Format("error: reqId={0} contractInfo={1}",
                                reqId,
                                contractInfo), Main.Common.Util.Constants.MessageType.Information);
        }

        public void error(int id, int errorCode, string errorMsg)
        {
            DoLog(string.Format("error: reqId={0} start={1} end={2}  ",
                                id,
                                errorCode,
                                errorMsg), Main.Common.Util.Constants.MessageType.Information);
        }

        public void error(string str)
        {
            DoLog(string.Format("error: str={0}   ",
                                str), Main.Common.Util.Constants.MessageType.Information);
        }

        public void error(Exception e)
        {
            DoLog(string.Format("error: ex={0}   ",
                                e.Message), Main.Common.Util.Constants.MessageType.Information);
        }

        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            DoLog(string.Format("execDetails: reqId={0} start={1} end={2}  ",
                                reqId,
                                contract.ToString(),
                                execution.ToString()), Main.Common.Util.Constants.MessageType.Information);
        }

        public void execDetailsEnd(int reqId)
        {
            DoLog(string.Format("execDetailsEnd: reqId={0}   ",
                                reqId), Main.Common.Util.Constants.MessageType.Information);
        }

        public void fundamentalData(int reqId, string data)
        {
            DoLog(string.Format("fundamentalData: reqId={0} data={1}  ",
                                reqId,
                                data), Main.Common.Util.Constants.MessageType.Information);
        }

        public void historicalData(int reqId, string date, double open, double high, double low, double close, int volume, int count, double WAP, bool hasGaps)
        {
            DoLog(string.Format("historicalData: reqId={0} date={1} open={2} high={3} low={4} close={5} close={6} volume={7} count={8} WAP={9} hasGaps={10}  ",
                                reqId,
                                date,
                                open,
                                high,
                                low,
                                close,
                                close,
                                volume,
                                count,
                                WAP,
                                hasGaps), Main.Common.Util.Constants.MessageType.Information);
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            DoLog(string.Format("historicalDataEnd: reqId={0} start={1} end={2}  ",
                                reqId,
                                start,
                                end), Main.Common.Util.Constants.MessageType.Information);
        }

        public void managedAccounts(string accountsList)
        {
            DoLog(string.Format("managedAccounts: accountsList={0}",
                                accountsList), Main.Common.Util.Constants.MessageType.Information);
        }

        public void marketDataType(int reqId, int marketDataType)
        {
            DoLog(string.Format("marketDataType: reqId={0} marketDataType={1} ",
                                reqId,
                                marketDataType
                                ), Main.Common.Util.Constants.MessageType.Information);
        }

        public void nextValidId(int orderId)
        {
            DoLog(string.Format("nextValidId: orderId={0}",
                                orderId), Main.Common.Util.Constants.MessageType.Information);
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            DoLog(string.Format("position: orderId={0} contract={1} order={2} orderState={3} ",
                                orderId,
                                contract.ToString(),
                                order.ToString(),
                                orderState.ToString()), Main.Common.Util.Constants.MessageType.Information);
        }

        public void openOrderEnd()
        {
            DoLog(string.Format("openOrderEnd "), Main.Common.Util.Constants.MessageType.Information);
        }

        public void orderStatus(int orderId, string status, int filled, int remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld)
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
                                whyHeld), Main.Common.Util.Constants.MessageType.Information);
        }

        public void position(string account, Contract contract, int pos, double avgCost)
        {
            DoLog(string.Format("position: account={0} contract={1} pos={2} avgCost={3} ",
                                account,
                                contract.ToString(),
                                pos,
                                avgCost), Main.Common.Util.Constants.MessageType.Information);
        }

        public void positionEnd()
        {
            DoLog(string.Format("positionEnd "), Main.Common.Util.Constants.MessageType.Information);
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
                                count), Main.Common.Util.Constants.MessageType.Information);
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            DoLog(string.Format("receiveFA: faDataType={0} faXmlData={1}  ",
                                 faDataType,
                                 faXmlData), Main.Common.Util.Constants.MessageType.Information);
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
                                legsStr), Main.Common.Util.Constants.MessageType.Information);
        }

        public void scannerDataEnd(int reqId)
        {
            DoLog(string.Format("scannerDataEnd: reqId={0}  ",
                                reqId), Main.Common.Util.Constants.MessageType.Information);
        }

        public void scannerParameters(string xml)
        {
            DoLog(string.Format("scannerParameters: xml={0}  ",
                                 xml), Main.Common.Util.Constants.MessageType.Information);
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
                                dividendsToExpiry), Main.Common.Util.Constants.MessageType.Information);
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            
            DoLog(string.Format("tickGeneric: tickerId={0} field={1} size={2} ",
                                 tickerId,
                                 field,
                                 value), Main.Common.Util.Constants.MessageType.Information);
            ProcessField("tickGeneric", tickerId, field, value);
        }

        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            DoLog(string.Format("tickOptionComputation: tickerId={0} field={1} impliedVolatility={2} delta={3} optPrice={4} pvDividend={5} gamma={6} vega={7} theta={8} undPrice={9} ",
                                tickerId,
                                field,
                                impliedVolatility,
                                delta,
                                optPrice,
                                pvDividend,
                                gamma,
                                vega,
                                theta,
                                undPrice), Main.Common.Util.Constants.MessageType.Information);
        }

        public void tickPrice(int tickerId, int field, double price, int canAutoExecute)
        {
            //DoLog(string.Format("tickPrice: tickerId={0} field={1} price={2} canAutoExecute={3}",
            //                    tickerId,
            //                    TickType.getField(field),
            //                    price,
            //                    canAutoExecute), Main.Common.Util.Constants.MessageType.Information);
            ProcessField("tickPrice", tickerId, field, price);
        }

        public void tickSize(int tickerId, int field, int size)
        {
            
            //DoLog(string.Format("tickSize: tickerId={0} field={1} size={2} ",
            //                     tickerId,
            //                     TickType.getField(field),
            //                     size), Main.Common.Util.Constants.MessageType.Information);
            ProcessField("tickSize", tickerId, field, size);
            
        }

        public virtual void tickSnapshotEnd(int tickerId)
        {
            DoLog(string.Format("tickSnapshotEnd: tickerId={0}",
                                 tickerId), Main.Common.Util.Constants.MessageType.Information);
        }

        public void tickString(int tickerId, int field, string value)
        {
            //DoLog(string.Format("tickString: tickerId={0} field={1} value={2} ",
            //                    tickerId,
            //                    TickType.getField(field),
            //                    value), Main.Common.Util.Constants.MessageType.Information);
            ProcessField("tickGeneric", tickerId, field, value);
        }

        public void updateAccountTime(string timestamp)
        {
            DoLog(string.Format("updateAccountTime: timestamp={0}",
                                 timestamp), Main.Common.Util.Constants.MessageType.Information);
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            DoLog(string.Format("updateAccountValue: key={0} value={1} currency={2} accountName={3}",
                                key,
                                value,
                                currency,
                                accountName), Main.Common.Util.Constants.MessageType.Information);
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            DoLog(string.Format("updateMktDepth: position={0} marketMaker={1} operation={2} side={3} price={4} size={5}",
                                position,
                                "",
                                operation,
                                side,
                                price,
                                size), Main.Common.Util.Constants.MessageType.Information);
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            DoLog(string.Format("updateMktDepthL2: position={0} marketMaker={1} operation={2} side={3} price={4} size={5}",
                                position,
                                marketMaker,
                                operation,
                                side,
                                price,
                                size), Main.Common.Util.Constants.MessageType.Information);

        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            DoLog(string.Format("updateNewsBulletin: msgType={0} message={1} origExchange={2}", msgType, message, origExchange), Main.Common.Util.Constants.MessageType.Information);

        }

        public void updatePortfolio(Contract contract, int position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName)
        {
            DoLog(string.Format("updatePortfolio: contract={0} marketValue={1}", contract.ToString(), marketValue), Main.Common.Util.Constants.MessageType.Information);
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            DoLog(string.Format("verifyCompleted: isSuccessfull={0} errorText={1}", isSuccessful, errorText), Main.Common.Util.Constants.MessageType.Information);
        }

        public void verifyMessageAPI(string apiData)
        {
            DoLog("verifyMessageAPI: " + apiData, Main.Common.Util.Constants.MessageType.Information);
        }

        #endregion
    }
}

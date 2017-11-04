using IBApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.IB.Common.DTO;

namespace zHFT.OrderRouters.IB.Common
{
    public abstract class OrderRouterBase : ICommunicationModule, EWrapper
    {
        #region Protected Attributes

        protected string ModuleConfigFile { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        protected OnMessageReceived OnMessageRcv { get; set; }

        protected IConfiguration Config { get; set; }

        protected int NextOrderId { get; set; }

        #endregion

        #region Abstract Methods

        public abstract CMState ProcessMessage(Wrapper wrapper);

        public abstract bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile);

        protected abstract void DoLoadConfig(string configFile, List<string> noValueFields);

        protected abstract CMState ProcessIncoming(Wrapper wrapper);

        protected abstract CMState ProcessOutgoing(Wrapper wrapper);

        protected abstract void ProcessOrderStatus(OrderStatusDTO dto);

        protected abstract void ProcessOrderError(int id, int errorCode, string errorMsg);

        #endregion

        #region Protected Methods

        protected void DoLog(string msg, Main.Common.Util.Constants.MessageType type)
        {
            if (OnLogMsg != null)
                OnLogMsg(msg, type);
        }

        protected bool LoadConfig(string configFile)
        {
            DoLog(DateTime.Now.ToString() + "OrderRouterBase.LoadConfig", Main.Common.Util.Constants.MessageType.Information);

            DoLog("Loading config:" + configFile, Main.Common.Util.Constants.MessageType.Information);
            if (!File.Exists(configFile))
            {
                DoLog(configFile + " does not exists", Main.Common.Util.Constants.MessageType.Error);
                return false;
            }

            List<string> noValueFields = new List<string>();
            DoLog("Processing config:" + configFile, Main.Common.Util.Constants.MessageType.Information);
            try
            {
                DoLoadConfig(configFile, noValueFields);
                DoLog("Ending GetConfiguracion " + configFile, Main.Common.Util.Constants.MessageType.Information);
            }
            catch (Exception e)
            {
                DoLog("Error recovering config " + configFile + ": " + e.Message, Main.Common.Util.Constants.MessageType.Error);
                return false;
            }

            if (noValueFields.Count > 0)
                noValueFields.ForEach(s => DoLog(string.Format(Main.Common.Util.Constants.FieldMissing, s), Main.Common.Util.Constants.MessageType.Error));

            return true;
        }

        #endregion

        #region IB Methods

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

        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            DoLog(string.Format("contractDetails: reqId={0} contractDetails={1}",
                                reqId,
                                contractDetails.ToString()), Main.Common.Util.Constants.MessageType.Information);
        }

        public void contractDetailsEnd(int reqId)
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

            ProcessOrderError(id, errorCode, errorMsg);
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
            NextOrderId = orderId;
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

            OrderStatusDTO dto = new OrderStatusDTO()
            {
                Id = orderId,
                Status = status,
                Filled = filled,
                Remaining = remaining,
                AvgFillPrice = avgFillPrice,
                PermId = permId,
                ParentId = parentId,
                LastFillPrice = lastFillPrice,
                CliendId = clientId,
                WhyHeld = whyHeld
            };

            ProcessOrderStatus(dto);

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
            DoLog(string.Format("tickPrice: tickerId={0} field={1} price={2} canAutoExecute={3}",
                                tickerId,
                                TickType.getField(field),
                                price,
                                canAutoExecute), Main.Common.Util.Constants.MessageType.Information);
        }

        public void tickSize(int tickerId, int field, int size)
        {

            DoLog(string.Format("tickSize: tickerId={0} field={1} size={2} ",
                                 tickerId,
                                 TickType.getField(field),
                                 size), Main.Common.Util.Constants.MessageType.Information);
        }

        public void tickSnapshotEnd(int tickerId)
        {
            DoLog(string.Format("tickSnapshotEnd: tickerId={0}",
                                 tickerId), Main.Common.Util.Constants.MessageType.Information);
        }

        public void tickString(int tickerId, int field, string value)
        {
            DoLog(string.Format("tickString: tickerId={0} field={1} value={2} ",
                                tickerId,
                                TickType.getField(field),
                                value), Main.Common.Util.Constants.MessageType.Information);
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

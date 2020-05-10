using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;


namespace zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers
{
    public class IBAccountManager : EWrapper, IAccountReferenceHandler
    {
        #region Private Static Consts

        private static string _ALL_ACCOUNTS = "All";

        private static string _SETTLED_CASH = "SettledCash";

        private static string _AVAILABLE_FUNDS = "AvailableFunds";

        private static string _PROCESS_SHORTS_KEY = "ProcessShorts";

        private static int _MAX_TIMEOUT_SECONDS = 20;

        #endregion


        #region Private Attributes

        protected EClientSocket ClientSocket { get; set; }

        protected static object tLock = new object();

        protected OnLogMessage  Logger { get; set; }

        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }

        protected bool AbortOnTimeout { get; set; }

        protected Account AccountToSync { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        protected List<ConfigKey> ConfigParameters { get; set; }

        #endregion

        #region IAccountReferenceHandler

        public Boolean ReadyAccountSummary()
        {
            return ReqAccountSummary;
        }

        public Boolean WaitingAccountPositions()
        {
            return ReqAccountPositions;
        }

        public bool IsAbortOnTimeout()
        {
            return AbortOnTimeout;
        }

        public Account GetAccountToSync()
        {
            return AccountToSync;
        }

        public List<AccountPosition> GetActivePositions()
        {
            bool processShorts = false;

            if (ConfigParameters.Any(x => x.Key == _PROCESS_SHORTS_KEY))
                processShorts = ConfigParameters.Where(x => x.Key == _PROCESS_SHORTS_KEY).FirstOrDefault().Value == "true";
            
            return Positions.Where(x => x.Shares.HasValue && (x.Shares.Value > 0 || processShorts)).ToList();
        }

        #endregion

        #region Constructors

        public IBAccountManager(OnLogMessage OnLogMsg,List<ConfigKey> pConfigParameters)
        {
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            Logger = OnLogMsg;
            ConfigParameters = pConfigParameters;

        }

        #endregion

        #region Protected Methods

        protected void TryDisconnect()
        {
            try
            {
                if (ClientSocket != null && ClientSocket.IsConnected())
                    ClientSocket.eDisconnect();
            }
            catch (Exception ex)
            {
                DoLog(ex.Message);
            }
            finally
            {

            }
        }

        protected void DoLog(string message)
        {
            Logger(message, Main.Common.Util.Constants.MessageType.Information);
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            DoLog(string.Format("accountSummary: reqId={0} account={1} tag={2} value={3} currency={4}  ",
                                reqId,
                                account,
                                tag,
                                value,
                                currency));

            try
            {
                if (tag == _AVAILABLE_FUNDS)
                {
                    AccountToSync.Balance = Convert.ToDecimal(value);
                    AccountToSync.Currency = currency;
                    ReqAccountSummary = false;
                }

            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical exception processing accountSummaryEnd: reqId={0} account={1} tag={2} value={3} currency={4}. Error={5}",
                                    reqId,
                                    account,
                                    tag,
                                    value,
                                    currency,
                                    ex.Message));
            }

        }

        public void accountSummaryEnd(int reqId)
        {
            DoLog(string.Format("accountSummaryEnd: reqId={0}", reqId));
        }

        public void error(int id, int errorCode, string errorMsg)
        {
            DoLog(string.Format("error: reqId={0} start={1} end={2}  ",
                                id,
                                errorCode,
                                errorMsg));
        }

        public void error(string str)
        {
            DoLog(string.Format("error: str={0}   ",
                                str));
        }

        public void error(Exception e)
        {
            DoLog(string.Format("error: ex={0}   ",
                                e.Message));
        }

        public void managedAccounts(string accountsList)
        {
            DoLog(string.Format("managedAccounts: ={0}   ", accountsList));
        }

        public void nextValidId(int orderId)
        {
        }

        public void position(string account, Contract contract, int pos, double avgCost)
        {
            try
            {
                if (account == AccountToSync.AccountDesc)
                {
                    DoLog(string.Format("Synchronizing position for symbol {0}   ", contract.Symbol));
                    AccountPosition accPos = new AccountPosition();
                    accPos.Account = AccountToSync;
                    accPos.Active = true;
                    accPos.Security = new Security() { Symbol = contract.Symbol };
                    accPos.Shares = pos;
                    accPos.PositionStatus = PositionStatus.GetNewPositionStatus(true);
                    accPos.MarketPrice = Convert.ToDecimal(avgCost);

                    if (accPos.MarketPrice.HasValue && accPos.Shares.HasValue)
                        accPos.Ammount = accPos.MarketPrice.Value * accPos.Shares.Value;

                    if(!Positions.Any(x=>x.Security.Symbol==contract.Symbol))
                        Positions.Add(accPos);
                }
            }
            catch (Exception ex)
            {

                DoLog(string.Format("Critical error sinchronyzing symbol {0}. Error={1}   ", contract.Symbol, ex.Message));
            }

        }

        public void positionEnd()
        {
            ReqAccountPositions = false;
        }

        public bool SyncAccountPositions(Account account)
        {
            if (account.URL == null || !account.Port.HasValue)
                throw new Exception("Debe especificar la URl y el puerto de la cuenta de Interactive Brokers para acceder a datos de la misma");


            try
            {
                lock (tLock)
                {
                    ClientSocket = new EClientSocket(this);
                    ClientSocket.eConnect(account.URL, Convert.ToInt32(account.Port), account.Id);
                    ReqAccountPositions = true;
                    AbortOnTimeout = false;
                    Positions = new List<AccountPosition>();
                    AccountToSync = account;
                    ClientSocket.reqPositions();
                    DateTime start = DateTime.Now;

                    while (ReqAccountPositions)
                    {
                        Thread.Sleep(100);

                        TimeSpan elapsed = DateTime.Now - start;

                        if (elapsed.TotalSeconds > _MAX_TIMEOUT_SECONDS || ReqAccountPositions == false)
                            break;
                    }
                }

                if (ClientSocket.IsConnected())
                {
                    TryDisconnect();
                    AbortOnTimeout = ReqAccountPositions;
                    return true;
                }
                else
                    throw new Exception("It was not possible to sync positions because the connection to TWS failed");

            }
            catch (Exception ex)
            {
                TryDisconnect();
                AbortOnTimeout = true;
                throw;
            }

        }

        public bool SyncAccountBalance(Account account)
        {
            if (account.URL == null || !account.Port.HasValue)
                throw new Exception("Debe especificar la URl y el puerto de la cuenta de Interactive Brokers para acceder a datos de la misma");
            try
            {
                lock (tLock)
                {
                    ClientSocket = new EClientSocket(this);
                    ClientSocket.eConnect(account.URL, Convert.ToInt32(account.Port), account.Id);
                    ReqAccountSummary = true;
                    AbortOnTimeout = false;
                    AccountToSync = account;
                    //ClientSocket.reqAccountSummary(account.Id, _ALL_ACCOUNTS, AccountSummaryTags.GetAllTags());
                    ClientSocket.reqAccountSummary(account.Id, _ALL_ACCOUNTS, _AVAILABLE_FUNDS);
                    DateTime start = DateTime.Now;

                    while (ReqAccountSummary)
                    {
                        Thread.Sleep(100);

                        TimeSpan elapsed = DateTime.Now - start;

                        if (elapsed.TotalSeconds > _MAX_TIMEOUT_SECONDS || ReqAccountSummary == false)
                            break;
                    }
                }

                if (ReqAccountSummary)
                    throw new Exception("Timeout completado en la sincronización del estado de la cuenta con Interactive Brokers. Por favor revise el estado de la conexión");

                if (ClientSocket.IsConnected())
                {
                    AbortOnTimeout = ReqAccountPositions;
                    TryDisconnect();
                    return true;
                }
                else
                    throw new Exception("It was not possible to sync account balanace because connection to TWS failed");
            
            }
            catch (Exception ex)
            {
                TryDisconnect();
                AbortOnTimeout = true;
                throw;
            }
        }

        #endregion

        #region Interface Members

        public void accountDownloadEnd(string account)
        {
            throw new NotImplementedException();
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            throw new NotImplementedException();
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            throw new NotImplementedException();
        }

        public void connectionClosed()
        {
            
        }

        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            throw new NotImplementedException();
        }

        public void contractDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void currentTime(long time)
        {
            throw new NotImplementedException();
        }

        public void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            throw new NotImplementedException();
        }

        public void displayGroupList(int reqId, string groups)
        {
            throw new NotImplementedException();
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            throw new NotImplementedException();
        }



        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            throw new NotImplementedException();
        }

        public void execDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void fundamentalData(int reqId, string data)
        {
            throw new NotImplementedException();
        }

        public void historicalData(int reqId, string date, double open, double high, double low, double close, int volume, int count, double WAP, bool hasGaps)
        {
            throw new NotImplementedException();
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            throw new NotImplementedException();
        }



        public void marketDataType(int reqId, int marketDataType)
        {
            throw new NotImplementedException();
        }



        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            throw new NotImplementedException();
        }

        public void openOrderEnd()
        {
            throw new NotImplementedException();
        }

        public void orderStatus(int orderId, string status, int filled, int remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld)
        {
            throw new NotImplementedException();
        }

        public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            throw new NotImplementedException();
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            throw new NotImplementedException();
        }

        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            throw new NotImplementedException();
        }

        public void scannerDataEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void scannerParameters(string xml)
        {
            throw new NotImplementedException();
        }

        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureExpiry, double dividendImpact, double dividendsToExpiry)
        {
            throw new NotImplementedException();
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            throw new NotImplementedException();
        }

        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            throw new NotImplementedException();
        }

        public void tickPrice(int tickerId, int field, double price, int canAutoExecute)
        {
            throw new NotImplementedException();
        }

        public void tickSize(int tickerId, int field, int size)
        {
            throw new NotImplementedException();
        }

        public void tickSnapshotEnd(int tickerId)
        {
            throw new NotImplementedException();
        }

        public void tickString(int tickerId, int field, string value)
        {
            throw new NotImplementedException();
        }

        public void updateAccountTime(string timestamp)
        {
            throw new NotImplementedException();
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            throw new NotImplementedException();
        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            throw new NotImplementedException();
        }

        public void updatePortfolio(Contract contract, int position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName)
        {
            throw new NotImplementedException();
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void verifyMessageAPI(string apiData)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

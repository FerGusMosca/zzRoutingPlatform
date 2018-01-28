using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.Enums;
using Binance.API.Csharp.Client.Models.Market;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.IBR.Binance.BusinessEntities;
using zHFT.StrategyHandler.IBR.Binance.Common.Util;
using zHFT.StrategyHandler.IBR.Binance.DataAccessLayer.Managers;
using zHFT.StrategyHandler.IBR.Cryptos.DataAccessLayer.Managers;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;


namespace zHFT.StrategyHandler.IBR.Binance.DataAccessLayer
{
    public class BinanceAccountManager : IAccountReferenceHandler
    {
        #region Protected Attributes

        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }

        protected bool AbortOnTimeout { get; set; }

        protected static object tLock = new object();

        protected OnLogMessage Logger { get; set; }

        protected Account AccountToSync { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        protected List<ConfigKey> ConfigParameters { get; set; }

        protected AccountBinanceData BinanceData { get; set; }

        protected string QuoteCurrency { get; set; }

        #endregion

        #region Private Consts

        private string _ACCOUNT_NUMBER = "AccountNumber";
        private string _CONFIG_CONNECTION_STRING = "ConfigConnectionString";
        private string _QUOTE_CURRENCY = "QuoteCurrency";

        private string _USD_CURRENCY = "USDT";
        private string _BTC_CURRENCY = "BTC";

        #endregion

        #region Constructors

        public BinanceAccountManager(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            Logger = OnLogMsg;
            ConfigParameters = pConfigParameters;
            ValidateDictionary();

            LoadConfig();
        }

        #endregion

        #region Protected Methods

        protected void ValidateDictionary()
        {

            if (ConfigParameters == null)
                throw new Exception("Config not specified for Binance Account Manager!");

            if (!ConfigParameters.Any(x => x.Key == _ACCOUNT_NUMBER))
                throw new Exception(string.Format("Config parameter not specified for Account Number!:{0}", _ACCOUNT_NUMBER));


            if (!ConfigParameters.Any(x => x.Key == _QUOTE_CURRENCY))
                throw new Exception(string.Format("Config parameter not specified for Binance Account Manager!:{0}", _QUOTE_CURRENCY));


            if (!ConfigParameters.Any(x => x.Key == _CONFIG_CONNECTION_STRING))
                throw new Exception(string.Format("Config parameter not specified for Binance Data Connection String!:{0}", _CONFIG_CONNECTION_STRING));

        }

        

        protected void LoadConfig()
        {
            string binanceConfigDataBaseCS=ConfigParameters.Where(x => x.Key == _CONFIG_CONNECTION_STRING).FirstOrDefault().Value;

            AccountBinanceDataManager accountBinanceDataManager = new AccountBinanceDataManager(binanceConfigDataBaseCS);
            
            int accountNumber = Convert.ToInt32(ConfigParameters.Where(x => x.Key == _ACCOUNT_NUMBER).FirstOrDefault().Value);

            QuoteCurrency=ConfigParameters.Where(x => x.Key == _QUOTE_CURRENCY).FirstOrDefault().Value;

            BinanceData = accountBinanceDataManager.GetByAccountNumber(accountNumber);

            if (BinanceData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al exchange Binance para la cuenta número {0}", accountNumber));


            AccountManager accountManager = new AccountManager(binanceConfigDataBaseCS);

            AccountToSync = accountManager.GetByAccountNumber(accountNumber);

            if (AccountToSync == null)
                throw new Exception(String.Format("No se encontró una cuenta para el número {0}", accountNumber));
        }

       
        private decimal GetBitcoinPriceInUSD()
        {
            var apiClient = new ApiClient(BinanceData.APIKey, BinanceData.Secret);
            //var binanceClient = new BinanceClient(apiClient);
            var binanceClientProxy = new BinanceClientProxy(apiClient);

            string fullSymbol = _BTC_CURRENCY + _USD_CURRENCY;

            //var resp = binanceClient.GetCandleSticks(fullSymbol, TimeInterval.Minutes_1, null, null,1);
            var resp = binanceClientProxy.GetLastMinuteCandleStick(fullSymbol);
            Candlestick jMarketData = resp.Result.OrderByDescending(x=>x.CloseTime).FirstOrDefault();

            return jMarketData.Close;
        }

        private void RecoverMarketPriceForPosition(ref AccountPosition pos, decimal priceBTCInUSD)
        {

            try
            {

                var apiClient = new ApiClient(BinanceData.APIKey, BinanceData.Secret);
                var binanceClient = new BinanceClientProxy(apiClient);

                if (pos.Security.Symbol != QuoteCurrency)
                {
                    string fullSymbol = pos.Security.Symbol + QuoteCurrency;

                    var resp = binanceClient.GetLastMinuteCandleStick(fullSymbol);
                    Candlestick jMarketData = resp.Result.OrderByDescending(x => x.CloseTime).FirstOrDefault();

                    pos.MarketPrice = jMarketData.Close * priceBTCInUSD;
                }
                else
                    pos.MarketPrice = priceBTCInUSD;
            }
            catch (Exception)
            {
                pos.MarketPrice = 0;//Si no se pudo recuperar, mala suerte
            }
        }

        #endregion

        #region Public Methods

        public bool SyncAccountPositions(Account account)
        {

            try
            {
                lock (tLock)
                {
                    ReqAccountPositions = true;
                    AbortOnTimeout = false;
                    Positions = new List<AccountPosition>();
                    AccountToSync = account;
                    //Pedir las posiciones y asignar

                    var apiClient = new ApiClient(BinanceData.APIKey, BinanceData.Secret);
                    var binanceClient = new BinanceClient(apiClient);

                    var asyncInfo = binanceClient.GetAccountInfo();

                    AccountInfo result = asyncInfo.Result;

                    decimal priceBTCInUSD = GetBitcoinPriceInUSD();

                    foreach (Balance balance in result.Balances)
                    {
                        decimal ammount = balance.Free + balance.Locked;
                        AccountPosition pos = new AccountPosition()
                        {
                            Account = AccountToSync,
                            Active=true,
                            PositionStatus=PositionStatus.GetNewPositionStatus(true),
                            Security = new Security() { Symbol = balance.Asset },
                            Ammount = ammount
                        };
                        if (pos.Ammount > 0)
                        {
                            RecoverMarketPriceForPosition(ref pos, priceBTCInUSD);
                            Positions.Add(pos);
                        }
                    }

                    ReqAccountPositions = false;

                    return true;
                }
            }
            catch (Exception ex)
            {
                AbortOnTimeout = true;
                throw;
            }
        }

        public bool SyncAccountBalance(Account account)
        {
            try
            {
                lock (tLock)
                {
                    ReqAccountSummary = true;
                    AbortOnTimeout = false;
                    AccountToSync = account;

                    AccountToSync.Balance = 0;//We have no USD balance in the crypto world (yet)

                    ReqAccountSummary = false;

                    DateTime start = DateTime.Now;

                    return true;
                  
                }
            }
            catch (Exception ex)
            {
                AbortOnTimeout = true;
                throw;
            }
        }

        public bool ReadyAccountSummary()
        {
            return ReqAccountSummary;
        }

        public bool WaitingAccountPositions()
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
            return Positions.Where(x => x.Ammount.HasValue && x.Ammount.Value > 0).ToList();
        }

        #endregion
    }
}

using Bittrex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.IBR.Bittrex.BusinessEntities;
using zHFT.StrategyHandler.IBR.Bittrex.DataAccessLayer.Managers;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.IBR.Bittrex.DataAccessLayer
{
    public class BittrexAccountManager : IAccountReferenceHandler
    {
        #region Protected Attributes

        protected Exchange Exchange { get; set; }

        protected ExchangeContext ExchangeContext { get; set; }

        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }

        protected bool AbortOnTimeout { get; set; }

        protected static object tLock = new object();

        protected OnLogMessage Logger { get; set; }

        protected Account AccountToSync { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        protected List<ConfigKey> ConfigParameters { get; set; }

        protected AccountBittrexData BittrexData { get; set; }

        #endregion

        #region Private Consts

        private string _ACCOUNT_NUMBER = "AccountNumber";
        private string _CONFIG_CONNECTION_STRING = "ConfigConnectionString";
        private string _QUOTE_CURRENCY = "QuoteCurrency";
        private string _SIMULATE = "Simulate";

        private string _USD_CURRENCY = "USDT";
        private string _BTC_CURRENCY = "BTC";

        #endregion

        #region Constructors

        public BittrexAccountManager(OnLogMessage OnLogMsg,List<ConfigKey> pConfigParameters )
        {
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            Logger = OnLogMsg;
            ConfigParameters = pConfigParameters;
            ValidateDictionary();

            LoadConfig();

            Exchange = new Exchange();

            ExchangeContext = GetContext();

            Exchange.Initialise(ExchangeContext);
        }

        #endregion

        #region Protected Methods

        protected void ValidateDictionary()
        {

            if (ConfigParameters == null)
                throw new Exception("Config not specified for Bittrex Account Manager!");

            if (!ConfigParameters.Any(x => x.Key == _ACCOUNT_NUMBER))
                throw new Exception(string.Format("Config parameter not specified for Account Number!:{0}", _ACCOUNT_NUMBER));


            if (!ConfigParameters.Any(x => x.Key == _QUOTE_CURRENCY))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Account Manager!:{0}", _QUOTE_CURRENCY));


            if (!ConfigParameters.Any(x => x.Key == _CONFIG_CONNECTION_STRING))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Data Connection String!:{0}", _CONFIG_CONNECTION_STRING));


            if (!ConfigParameters.Any(x => x.Key == _SIMULATE))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Account Manager!:{0}", _SIMULATE));

            try
            {
                bool test = Convert.ToBoolean(ConfigParameters.Where(x => x.Key == _SIMULATE).FirstOrDefault().Value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Invalid formar for config parameter {0} for Bittrex Account Manager!:{1}", _SIMULATE, ex.Message));
            }
        }

        protected void LoadConfig()
        {
            string bittrexConfigDataBaseCS=ConfigParameters.Where(x => x.Key == _CONFIG_CONNECTION_STRING).FirstOrDefault().Value;

            AccountBittrexDataManager accountBittrexDataManager = new AccountBittrexDataManager(bittrexConfigDataBaseCS);
            
            int accountNumber = Convert.ToInt32(ConfigParameters.Where(x => x.Key == _ACCOUNT_NUMBER).FirstOrDefault().Value);

            BittrexData = accountBittrexDataManager.GetByAccountNumber(accountNumber);

            if (BittrexData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al exchange Bittrex para la cuenta número {0}", accountNumber));

        }

        protected ExchangeContext GetContext()
        {
            return new ExchangeContext()
            {
                ApiKey = BittrexData.APIKey,
                QuoteCurrency = ConfigParameters.Where(x => x.Key == _QUOTE_CURRENCY).FirstOrDefault().Value,
                Secret = BittrexData.Secret,
                Simulate = Convert.ToBoolean(ConfigParameters.Where(x => x.Key == _SIMULATE).FirstOrDefault().Value)
            };
        }

        public bool MarketExists(string quoteCurrency, string currency)
        {
            Exchange exchange = new Exchange();

            ExchangeContext ctx = new ExchangeContext();
            ctx.QuoteCurrency = quoteCurrency;

            exchange.Initialise(ctx);

            try
            {
                GetMarketSummaryResponse resp = exchange.GetMarketSummary(currency);

                return resp != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public decimal GetBitcoinPriceInUSD()
        {
            Exchange exch = new Exchange();
            ExchangeContext ctx = GetContext();
            ctx.QuoteCurrency = _USD_CURRENCY;
            exch.Initialise(ctx);

            GetMarketSummaryResponse summary = exch.GetMarketSummary(_BTC_CURRENCY);

            return summary.Last;
           
        }

        protected void RecoverMarketPriceForPosition(ref AccountPosition pos, decimal btcPriceInUSD)
        {
            try
            {
                if (MarketExists(ExchangeContext.QuoteCurrency, pos.Security.Symbol))
                {
                    GetMarketSummaryResponse summary = Exchange.GetMarketSummary(pos.Security.Symbol);
                    pos.MarketPrice = summary.Last;

                }
                else if (MarketExists(_BTC_CURRENCY, pos.Security.Symbol))
                {
                    Exchange revExch = new Exchange();
                    ExchangeContext ctx = GetContext();
                    ctx.QuoteCurrency = _BTC_CURRENCY;
                    revExch.Initialise(ctx);
                    //Pedimos el precio en Bitcoins
                    GetMarketSummaryResponse summary = revExch.GetMarketSummary(pos.Security.Symbol);

                    pos.MarketPrice = btcPriceInUSD * summary.Last;
                }
                else if (MarketExists(pos.Security.Symbol, _BTC_CURRENCY))
                {
                    Exchange revExch = new Exchange();
                    ExchangeContext ctx = GetContext();
                    ctx.QuoteCurrency = pos.Security.Symbol;
                    revExch.Initialise(ctx);
                    //Pedimos el precio en Bitcoins
                    GetMarketSummaryResponse summary = revExch.GetMarketSummary(_BTC_CURRENCY);
                    pos.MarketPrice = btcPriceInUSD * (1 / summary.Last);
                }
                else
                    pos.MarketPrice = 0;
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
                    GetBalancesResponse resp =  Exchange.GetBalances();

                    decimal btcPriceInUSD = GetBitcoinPriceInUSD();

                    foreach (AccountBalance balance in resp)
                    {
                        if (balance.Balance > 0)
                        {
                            AccountPosition pos = new AccountPosition();

                            pos.Account = account;
                            pos.Active = true;
                            pos.PositionStatus = PositionStatus.GetNewPositionStatus(true);
                            pos.Security = new Security() { Symbol = balance.Currency };
                            pos.Ammount = balance.Balance;

                            RecoverMarketPriceForPosition(ref pos, btcPriceInUSD);

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

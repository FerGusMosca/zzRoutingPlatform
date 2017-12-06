using Bittrex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
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

        #endregion

        #region Private Consts

        private string _API_KEY = "ApiKey";
        private string _QUOTE_CURRENCY = "QuoteCurrency";
        private string _SECRET = "Secret";
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

            if (!ConfigParameters.Any(x=>x.Key==_API_KEY))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Account Manager!:{0}", _API_KEY));


            if (!ConfigParameters.Any(x => x.Key == _QUOTE_CURRENCY))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Account Manager!:{0}", _QUOTE_CURRENCY));


            if (!ConfigParameters.Any(x => x.Key == _SECRET))
                throw new Exception(string.Format("Config parameter not specified for Bittrex Account Manager!:{0}", _SECRET));


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

        protected ExchangeContext GetContext()
        {
            return new ExchangeContext()
            {
                ApiKey = ConfigParameters.Where(x => x.Key == _API_KEY).FirstOrDefault().Value,
                QuoteCurrency = ConfigParameters.Where(x => x.Key == _QUOTE_CURRENCY).FirstOrDefault().Value,
                Secret = ConfigParameters.Where(x => x.Key == _SECRET).FirstOrDefault().Value,
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

        protected void RecoverMarketPriceForPosition(ref AccountPosition pos)
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

                    pos.MarketPrice = GetBitcoinPriceInUSD() * summary.Last;
                }
                else if (MarketExists(pos.Security.Symbol, _BTC_CURRENCY))
                {
                    Exchange revExch = new Exchange();
                    ExchangeContext ctx = GetContext();
                    ctx.QuoteCurrency = pos.Security.Symbol;
                    revExch.Initialise(ctx);
                    //Pedimos el precio en Bitcoins
                    GetMarketSummaryResponse summary = revExch.GetMarketSummary(_BTC_CURRENCY);
                    pos.MarketPrice = GetBitcoinPriceInUSD() * (1 / summary.Last);
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

                    foreach (AccountBalance balance in resp)
                    {
                        AccountPosition pos = new AccountPosition();

                        pos.Account = account;
                        pos.Active = true;
                        pos.PositionStatus = PositionStatus.GetNewPositionStatus(true);
                        pos.Security = new Security() { Symbol = balance.Currency };
                        pos.Ammount = balance.Available;

                        RecoverMarketPriceForPosition(ref pos);

                        Positions.Add(pos);
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

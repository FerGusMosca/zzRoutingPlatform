using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.IBR.Primary.BusinessEntities;
using zHFT.StrategyHandler.IBR.Primary.Common.DTO;
using zHFT.StrategyHandler.IBR.Primary.DataAccessLayer.Managers;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.IBR.Primary.DataAccessLayer
{
    public class PrimaryAccountManager : IAccountReferenceHandler
    {

        #region Constructors

        public PrimaryAccountManager(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            Logger = OnLogMsg;
            ConfigParameters = pConfigParameters;
            ValidateDictionary();
            LoadConfig();
        }

        #endregion

        #region Private Consts

        private string _ACCOUNT_NUMBER = "AccountNumber";

        private string _CONFIG_CONNECTION_STRING = "ConfigConnectionString";

        private string _DETAIL_POSITION_URL = "DetailPositionsURL";

        private string _ARS_CURRENCY = "ARS";

        private string _ST_STOCK = "STOCK";

        private string _ST_BOND = "BOND";

        private string _ST_FUTURE = "FUTURE";

        private string _ST_OPTION_PUT = "OPTION_PUT";

        private string _ST_OPTION_CALL = "OPTION_CALL";

        private string _ST_CEDEAR = "CEDEAR";

        private string _BUE = "BUE";

        private string _ROFX = "ROFX";

        #endregion


        #region Protected Attributes

        public Account Account { get; set; }

        protected OnLogMessage Logger { get; set; }

        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }

        protected List<ConfigKey> ConfigParameters { get; set; }

        protected AccountPrimaryData AccountPrimaryData { get; set; }

        protected string DetailPositonURL { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        #endregion

        #region Private Methods

        protected string GetFullSymbol(string secType, string symbol)
        {
            if (secType == _ST_STOCK)
                return string.Format("{0}.{1}", symbol, _BUE);
            else if (secType == _ST_BOND)
                return string.Format("{0}.{1}", symbol, _BUE);
            else if (secType == _ST_FUTURE)
                return string.Format("{0}.{1}", symbol, _ROFX);
            else if (secType == _ST_OPTION_PUT)
                return symbol;
            else if (secType == _ST_OPTION_CALL)
                return symbol;
            else if (secType == _ST_CEDEAR)
                return string.Format("{0}.{1}", symbol, _BUE);
            else
                return symbol;
        }

        protected void ValidateDictionary()
        {

            if (ConfigParameters == null)
                throw new Exception("Config not specified for Primary Account Manager!");

            if (!ConfigParameters.Any(x => x.Key == _ACCOUNT_NUMBER))
                throw new Exception(string.Format("Config parameter not specified for Account Number!:{0}", _ACCOUNT_NUMBER));

            if (!ConfigParameters.Any(x => x.Key == _CONFIG_CONNECTION_STRING))
                throw new Exception(string.Format("Config parameter not specified for Primary Data Connection String!:{0}", _CONFIG_CONNECTION_STRING));

            if (!ConfigParameters.Any(x => x.Key == _DETAIL_POSITION_URL))
                throw new Exception(string.Format("Config parameter not specified :{0}", _DETAIL_POSITION_URL));

        }

        protected void LoadConfig()
        {
            string primaryConfigDataBaseCS = ConfigParameters.Where(x => x.Key == _CONFIG_CONNECTION_STRING).FirstOrDefault().Value;

            ADOAccountPrimaryDataManager accountPrimaryDataManager = new ADOAccountPrimaryDataManager(primaryConfigDataBaseCS);

            int accountNumber = Convert.ToInt32(ConfigParameters.Where(x => x.Key == _ACCOUNT_NUMBER).FirstOrDefault().Value);

            DetailPositonURL = ConfigParameters.Where(x => x.Key == _DETAIL_POSITION_URL).FirstOrDefault().Value;

            AccountPrimaryData = accountPrimaryDataManager.GetAccountPrimaryData(accountNumber);

            if (AccountPrimaryData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al proveedor de datos Primary para la cuenta número {0}", accountNumber));

        }

        protected string DoLogin(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.Headers["X-Username"] = AccountPrimaryData.User;
            request.Headers["X-Password"] = AccountPrimaryData.Password;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.Headers.AllKeys.Contains("X-Auth-Token"))
                return response.Headers["X-Auth-Token"];
            else
                return null;
        }

        protected string DoGetJson(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //request.Headers["X-Username"] = AccountPrimaryData.User;
            //request.Headers["X-Password"] = AccountPrimaryData.Password;
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(string.Format("{0}:{1}", AccountPrimaryData.User, AccountPrimaryData.Password)));


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string content = string.Empty;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }
            }
            return content;
        }

        #endregion

        #region Public Methods
        public bool SyncAccountPositions(Account account)
        {
            Account = account;
            Positions = new List<AccountPosition>();

            string url = string.Format("{0}{1}", DetailPositonURL, account.GenericAccountNumber);
            string resp = DoGetJson(url);

            DetailedPositionResponse detailedPositionResp = JsonConvert.DeserializeObject<DetailedPositionResponse>(resp);

            foreach (var secType in detailedPositionResp.report.Keys)
            {
                Dictionary<string, DetailedPositions> detailsPositionsForSecType = detailedPositionResp.report[secType];

                foreach (string symbol in detailsPositionsForSecType.Keys)
                {
                    DetailedPositions pos = detailsPositionsForSecType[symbol];

                    AccountPosition accPos = new AccountPosition();
                    accPos.Account = Account;
                    accPos.Active = true;
                    accPos.Security = new Security() { Symbol = GetFullSymbol(secType, symbol) };
                    accPos.Shares = pos.instrumentCurrentSize.HasValue ? (int?) Convert.ToInt32(pos.instrumentCurrentSize) : null;
                    accPos.PositionStatus = PositionStatus.GetNewPositionStatus(true);
                    accPos.Ammount = pos.instrumentMarketValue;

                    if (accPos.Shares > 0)
                    {
                        if (accPos.Shares.HasValue && accPos.Ammount.HasValue)
                            accPos.MarketPrice = accPos.Ammount / accPos.Shares;

                        if (!Positions.Any(x => x.Security.Symbol == symbol))
                            Positions.Add(accPos);
                    }
                }
            }

            return true;
        }

        public bool SyncAccountBalance(InstructionBasedRouting.BusinessEntities.Account account)
        {
            Account = account;
            //string token = DoLogin(AuthURL);
            string url = string.Format("{0}{1}", DetailPositonURL, account.GenericAccountNumber);

            string resp = DoGetJson(url);

            DetailedPositionResponse detailedPositionResp = JsonConvert.DeserializeObject<DetailedPositionResponse>(resp);


            Account.Balance = Convert.ToDecimal(detailedPositionResp.totalMarketValue);
            Account.Currency = _ARS_CURRENCY;

            return true;
        }

        public bool ReadyAccountSummary()
        {
            return false;
        }

        public bool WaitingAccountPositions()
        {
            return false;
        }

        public bool IsAbortOnTimeout()
        {
            return false;
        }

        public Account GetAccountToSync()
        {
            return Account;
        }

        public List<AccountPosition> GetActivePositions()
        {
            return Positions.Where(x => x.Shares.HasValue && x.Shares.Value != 0).ToList();
        }

        #endregion
    }
}

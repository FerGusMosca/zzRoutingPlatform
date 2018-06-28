using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.IBR.IOL.BusinessEntities;
using zHFT.StrategyHandler.IBR.IOL.Common.DTO;
using zHFT.StrategyHandler.IBR.IOL.DataAccessLayer.ADO;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.IBR.IOL.DataAccessLayer
{
    public class IOLAccountManager : IAccountReferenceHandler
    {
        #region Constructors

        public IOLAccountManager(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            Logger = OnLogMsg;
            ConfigParameters = pConfigParameters;
            //ValidateDictionary();
            LoadConfig();
            Authenticate();
        }

        #endregion

        #region Private Consts

        private string _ACCOUNT_NUMBER = "AccountNumber";

        private string _CONFIG_CONNECTION_STRING = "ConfigConnectionString";

        private string _LOGIN_TOKEN_URL = "https://api.invertironline.com/token";

        #endregion

        #region Protected Methods

        protected void ValidateDictionary()
        {

            if (ConfigParameters == null)
                throw new Exception("Config not specified for Invertir Online Account Manager!");

            if (!ConfigParameters.Any(x => x.Key == _ACCOUNT_NUMBER))
                throw new Exception(string.Format("Config parameter not specified for Account Number!:{0}", _ACCOUNT_NUMBER));

            if (!ConfigParameters.Any(x => x.Key == _CONFIG_CONNECTION_STRING))
                throw new Exception(string.Format("Config parameter not specified for Invertir Online Data Connection String!:{0}", _CONFIG_CONNECTION_STRING));

        }

        protected void LoadConfig()
        {
            string iolConfigDataBaseCS = ConfigParameters.Where(x => x.Key == _CONFIG_CONNECTION_STRING).FirstOrDefault().Value;

            ADOAccountIOLDataManager accountIOLDataManager = new ADOAccountIOLDataManager(iolConfigDataBaseCS);

            int accountNumber = Convert.ToInt32(ConfigParameters.Where(x => x.Key == _ACCOUNT_NUMBER).FirstOrDefault().Value);

            AccountInvertirOnlineData = accountIOLDataManager.GetAccountPrimaryData(accountNumber);

            if (AccountInvertirOnlineData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al proveedor de datos Invertir Online para la cuenta número {0}", accountNumber));

        }


        protected void Authenticate()
        {
            string url = _LOGIN_TOKEN_URL;
            string postData = string.Format("username={0}&password={1}&grant_type=password", AccountInvertirOnlineData.User, AccountInvertirOnlineData.Password);
            var data = Encoding.ASCII.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            try
            {

                var response = (HttpWebResponse)request.GetResponse();

                var resp = new StreamReader(response.GetResponseStream()).ReadToEnd();

                AuthenticationToken = JsonConvert.DeserializeObject<AuthenticationToken>(resp);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Could not authenticate user {0} on Invertir Online", AccountInvertirOnlineData.User));
            }
        }

        #endregion

        #region Protected Attributes

        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }

        protected OnLogMessage Logger { get; set; }

        protected List<ConfigKey> ConfigParameters { get; set; }

        protected AccountInvertirOnlineData AccountInvertirOnlineData { get; set; }

        protected AuthenticationToken AuthenticationToken { get; set; }

        #endregion

        #region IAccountReferenceHandler Methods

        public bool SyncAccountPositions(Account account)
        {
            throw new NotImplementedException();
        }

        public bool SyncAccountBalance(Account account)
        {
            throw new NotImplementedException();
        }

        public bool ReadyAccountSummary()
        {
            throw new NotImplementedException();
        }

        public bool WaitingAccountPositions()
        {
            throw new NotImplementedException();
        }

        public bool IsAbortOnTimeout()
        {
            throw new NotImplementedException();
        }

        public Account GetAccountToSync()
        {
            throw new NotImplementedException();
        }

        public List<AccountPosition> GetActivePositions()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Public Methods


        #endregion
    }
}

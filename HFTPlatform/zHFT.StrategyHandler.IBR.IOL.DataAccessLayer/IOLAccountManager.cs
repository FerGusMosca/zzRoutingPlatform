using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.Common;
using zHFT.InstrFullMarketConnectivity.IOL.DataAccessLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.IBR.IOL.Common.DTO;
using zHFT.StrategyHandler.IBR.IOL.DataAccessLayer.ADO;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.IBR.IOL.DataAccessLayer
{
    public class IOLAccountManager : BaseManager, IAccountReferenceHandler
    {
        #region Constructors

        public IOLAccountManager(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            AbortOnTimeout = false;
            AccountToSync = new Account();
            Positions = new List<AccountPosition>();
            Logger = OnLogMsg;
            ConfigParameters = pConfigParameters;
            //ValidateDictionary();
            LoadConfig();

            Logger("Authenticating Account Manager On Invertir Online", Main.Common.Util.Constants.MessageType.Information);
            Authenticate();
            Logger(string.Format("Account Manageauthenticated On Invertir Online. Token:{0}", AuthenticationToken.access_token),
                   Main.Common.Util.Constants.MessageType.Information);
        }

        #endregion

        #region Protected Consts

        protected string _PORTFOLIO_URL = "api/portafolio";

        #endregion

        #region Private Consts

        private string _ACCOUNT_NUMBER = "AccountNumber";

        private string _CONFIG_CONNECTION_STRING = "ConfigConnectionString";

        private string _MAIN_URL = "MainURL";

        #endregion

        #region IOL Consts

        private static string _ARS = "ARS";

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

            if (!ConfigParameters.Any(x => x.Key == _LOGIN_TOKEN_URL))
                throw new Exception(string.Format("Config parameter not specified for Invertir Online !:{0}", _LOGIN_TOKEN_URL));

        
        }

        protected void LoadConfig()
        {

            MainURL = ConfigParameters.Where(x => x.Key == _MAIN_URL).FirstOrDefault().Value;

            string iolConfigDataBaseCS = ConfigParameters.Where(x => x.Key == _CONFIG_CONNECTION_STRING).FirstOrDefault().Value;

            ADOAccountIOLDataManager accountIOLDataManager = new ADOAccountIOLDataManager(iolConfigDataBaseCS);

            int accountNumber = Convert.ToInt32(ConfigParameters.Where(x => x.Key == _ACCOUNT_NUMBER).FirstOrDefault().Value);

            AccountInvertirOnlineData = accountIOLDataManager.GetAccountIolData(accountNumber);

            if (AccountInvertirOnlineData == null)
                throw new Exception(string.Format("No se encontró la configuración de acceso al proveedor de datos Invertir Online para la cuenta número {0}", accountNumber));

        }

        #endregion

        #region Protected Attributes

        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }

        protected OnLogMessage Logger { get; set; }

        protected List<ConfigKey> ConfigParameters { get; set; }

        public Account AccountToSync { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        protected bool AbortOnTimeout { get; set; }

        protected object tLock = new object();

        #endregion

        #region Private Methods

        private void LoadAccountPosition(Posicion[] positions)
        {
            foreach (Posicion pos in positions)
            {
                AccountPosition accPos = new AccountPosition();

                accPos.Account = AccountToSync;
                accPos.Shares = Convert.ToInt32(pos.cantidad);
                accPos.PositionStatus = PositionStatus.GetNewPositionStatus(true);
                accPos.Active = true;
                accPos.MarketPrice = pos.ultimoPrecio;
                accPos.Ammount = pos.valorizado;

                string exchange = ExchangeConverter.GetInstrMarketFromIolMarket(pos.titulo.mercado);

                accPos.Security = new Security()
                {
                    Symbol = pos.titulo.simbolo + "." + exchange,
                    SecurityDesc = pos.titulo.descripcion,
                    Exchange = pos.titulo.mercado,
                    Currency = pos.titulo.moneda
                    //TEC cargar tipo y plazo
                };

                accPos.Security.MarketData = new MarketData()
                {
                    Trade = pos.ultimoPrecio.HasValue ? Convert.ToDouble(pos.ultimoPrecio) : (double?) null,
                };

                Positions.Add(accPos);
            }

        }

        #endregion

        #region IAccountReferenceHandler Methods

        public bool SyncAccountPositions(Account account)
        {
            try
            {
                lock (tLock)
                {
                    if (AuthenticationToken == null)
                        Authenticate();

                    string url = MainURL + _PORTFOLIO_URL;
                    Positions = new List<AccountPosition>();
                    AccountToSync = account;
                    string resp = DoGetJson(url);
                    PortfolioResponse portfResp = JsonConvert.DeserializeObject<PortfolioResponse>(resp);
                    LoadAccountPosition(portfResp.activos);
                }
                    
                return true;
               
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
                    if (AuthenticationToken == null)
                        Authenticate();

                    string url = MainURL + _PORTFOLIO_URL;
                    Positions = new List<AccountPosition>();
                    AccountToSync = account;
                    string resp = DoGetJson(url);
                    PortfolioResponse portfResp = JsonConvert.DeserializeObject<PortfolioResponse>(resp);


                    decimal balance = 0;
                    foreach (Posicion activo in portfResp.activos)
                        balance += activo.valorizado.HasValue ? activo.valorizado.Value : 0;

                    AccountToSync.Balance = balance;
                    AccountToSync.Currency = _ARS;//TODO: Implementar traducción de moneda en base a lo que se recupere de posiciones @IOL
                }

                return true;

            }
            catch (Exception ex)
            {
                AbortOnTimeout = true;
                throw;
            }
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
            return AbortOnTimeout;
        }

        public Account GetAccountToSync()
        {
            return AccountToSync;
        }

        public List<AccountPosition> GetActivePositions()
        {
            return Positions.Where(x => x.Shares.HasValue && x.Shares.Value != 0).ToList();
        }

        #endregion

    }
}

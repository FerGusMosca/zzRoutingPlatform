using System;
using System.Collections.Generic;
using System.Linq;
using tph.OrderRouter.Cocos.Common.DTO.Accounts;
using tph.OrderRouter.ServiceLayer;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace tph.StrategyHandler.IBR.Cocos.ServiceLayer
{
    public class CocosAccountServiceClient : BaseServiceClient, IAccountReferenceHandler
    {

        #region Constructors

        public CocosAccountServiceClient(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            Name = "Invertir Online Account Reference Handler";
            Logger = OnLogMsg;
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            AbortOnTimeout = false;
            AccountToSync = new Account();
            Positions = new List<AccountPosition>();

            ConfigParameters = pConfigParameters;

            Logger("Authenticating Account Manager On Cocos", Constants.MessageType.Information);
            InitializeCocosClient(pConfigParameters);
            SyncAccountPositions(null);
            Logger(string.Format("Account Manager authenticated On Cocos"), Constants.MessageType.Information);
        }

        #endregion

        #region Private Consts

        private static string _BASE_URL_KEY = "BASE_URL";
        private static string _DNI_KEY = "DNI";
        private static string _USER_KEY = "USER";
        private static string _PWD_KEY = "PWD";
        private static string _ACCOUNT_KEY = "ACCOUNT";

        private static string _SUBTOTAL_ACCIONES_KEY = "Subtotal Acciones";

        #endregion

        #region Protected Attributes

        protected CocosOrderRouterServiceClient CocosOrderRouterServiceClient { get; set; }

        protected List<ConfigKey> ConfigParameters { get; set; }

        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }

        protected string Name { get; set; }

        protected string Account { get; set; }

        protected OnLogMessage Logger { get; set; }

        protected bool AbortOnTimeout { get; set; }

        public Account AccountToSync { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        #endregion

        #region Public Methods

        public bool SyncAccountPositions(Account account)
        {
            Positions posdto = CocosOrderRouterServiceClient.GetPositions(Account);

            Positions = new List<AccountPosition>();

            ActivoPosition assetPositions =
                posdto.Result.Activos.Where(x => x.ESPE == _SUBTOTAL_ACCIONES_KEY).FirstOrDefault();

            if (assetPositions != null)
            {
                foreach (SubtotalPosition innerPos in assetPositions.Subtotal)
                {
                    if (innerPos.CANT.HasValue && innerPos.CANT.Value > 0)
                    {
                        AccountPosition accPos = new AccountPosition()
                        {
                            Account = account,
                            Active = true,
                            Security = new Security()
                            {
                                Symbol = innerPos.TICK,
                                SecType = SecurityType.OTH,
                                SecurityDesc = innerPos.AMPL
                            },
                            Shares = innerPos.CANT.Value,
                            Weight = 0,
                            MarketPrice = GetSafeDecimal(innerPos.PCIO), 
                        };

                        accPos.Ammount = accPos.Shares * accPos.MarketPrice;

                        Positions.Add(accPos);
                    }
                }
            }

            return true;
        }

        public bool SyncAccountBalance(Account account)
        {
            //TODO: dev sync

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
            return AbortOnTimeout;
        }

        public Account GetAccountToSync()
        {
            return AccountToSync;
        }

        public List<AccountPosition> GetActivePositions()
        {
            return Positions;
        }

        #endregion

        #region Private Methods

        private decimal GetSafeDecimal(string dbl)
        {
            try
            {
                return Convert.ToDecimal(dbl);
            }
            catch (Exception e)
            {
                return 0;
            }
        }
    

    public void InitializeCocosClient(List<ConfigKey> pConfigParameters)
        {
            string baseURL = "";
            if (pConfigParameters.Any(x => x.Key == _BASE_URL_KEY))
                baseURL = pConfigParameters.Where(x => x.Key == _BASE_URL_KEY).FirstOrDefault().Value;
            else
                throw new Exception(string.Format("Missing {0} key in Cocos strategy config file",_BASE_URL_KEY));
            
            string DNI = "";
            if (pConfigParameters.Any(x => x.Key == _DNI_KEY))
                DNI = pConfigParameters.Where(x => x.Key == _DNI_KEY).FirstOrDefault().Value;
            else
                throw new Exception(string.Format("Missing {0} key in Cocos strategy config file",_DNI_KEY));
            
            string user = "";
            if (pConfigParameters.Any(x => x.Key == _USER_KEY))
                user = pConfigParameters.Where(x => x.Key == _USER_KEY).FirstOrDefault().Value;
            else
                throw new Exception(string.Format("Missing {0} key in Cocos strategy config file",_USER_KEY));
            
            string pPassword = "";
            if (pConfigParameters.Any(x => x.Key == _PWD_KEY))
                pPassword = pConfigParameters.Where(x => x.Key == _PWD_KEY).FirstOrDefault().Value;
            else
                throw new Exception(string.Format("Missing {0} key in Cocos strategy config file",_PWD_KEY));

            Account = "";
            if (pConfigParameters.Any(x => x.Key == _ACCOUNT_KEY))
                Account = pConfigParameters.Where(x => x.Key == _ACCOUNT_KEY).FirstOrDefault().Value;
            else
                throw new Exception(string.Format("Missing {0} key in Cocos strategy config file",_ACCOUNT_KEY));
            
            CocosOrderRouterServiceClient = new CocosOrderRouterServiceClient(baseURL, DNI, user, pPassword);

        }

        #endregion
    }
}
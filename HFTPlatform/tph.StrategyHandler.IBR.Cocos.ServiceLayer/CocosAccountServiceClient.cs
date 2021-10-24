using System;
using System.Collections.Generic;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace tph.StrategyHandler.IBR.Cocos.ServiceLayer
{
    public class CocosAccountClient: BaseServiceClient,IAccountReferenceHandler
    {
        
        #region Constructors

        public CocosAccountClient(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            Name = "Invertir Online Account Reference Handler";
            Logger = OnLogMsg;
            ReqAccountSummary = false;
            ReqAccountPositions = false;
            AbortOnTimeout = false;
            AccountToSync = new Account();
            Positions = new List<AccountPosition>();
            
            ConfigParameters = pConfigParameters;

            Logger("Authenticating Account Manager On Cocos",Constants.MessageType.Information);
            DoAuthenticate();
            Logger(string.Format("Account Manager authenticated On Cocos"),Constants.MessageType.Information);
        }

        #endregion
        
        #region Protected Attributes
        
        protected List<ConfigKey> ConfigParameters { get; set; }
        
        protected Boolean ReqAccountSummary { get; set; }

        protected Boolean ReqAccountPositions { get; set; }
        
        protected string Name { get; set; }

        protected OnLogMessage Logger { get; set; }
        
        protected bool AbortOnTimeout { get; set; }
        
        public Account AccountToSync { get; set; }
        
        protected List<AccountPosition> Positions { get; set; }
        
        #endregion
        
        #region Public Attributes
        
        public bool SyncAccountPositions(Account account)
        {
            //TODO: dev sync

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
    }
}
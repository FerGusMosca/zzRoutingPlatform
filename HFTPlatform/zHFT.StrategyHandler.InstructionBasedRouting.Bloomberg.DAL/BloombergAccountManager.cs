using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Bloomberg.DAL.Managers
{
    public class BloombergAccountManager : IAccountReferenceHandler
    {
        #region Public Attribute

        protected bool AbortOnTimeout { get; set; }

        protected Account AccountToSync { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        #endregion

        #region Constructors

        public BloombergAccountManager(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            Positions = new List<AccountPosition>();

            AccountToSync = new Account();
        }

        #endregion

        #region Public Methods

        public bool SyncAccountPositions(BusinessEntities.Account account)
        {
            AbortOnTimeout = false;
            return true;
        }

        public bool SyncAccountBalance(BusinessEntities.Account account)
        {
            AbortOnTimeout = false;
            return true;
        }

        public bool ReadyAccountSummary()
        {
            return true;
        }

        public bool WaitingAccountPositions()
        {
            return false;
        }

        public bool IsAbortOnTimeout()
        {
            return AbortOnTimeout;
        }

        public BusinessEntities.Account GetAccountToSync()
        {
            return AccountToSync;
        }

        public List<BusinessEntities.AccountPosition> GetActivePositions()
        {
            return Positions;
        }

        #endregion
    }
}

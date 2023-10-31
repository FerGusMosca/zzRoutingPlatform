using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Configuration;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.IBR.Bitstamp.DataAccessLayer.Managers
{
    public class BitstampAccountManager : IAccountReferenceHandler
    {
        #region Public Attribute

        protected bool AbortOnTimeout { get; set; }

        protected Account AccountToSync { get; set; }

        protected List<AccountPosition> Positions { get; set; }

        #endregion

        #region Constructors

        public BitstampAccountManager(OnLogMessage OnLogMsg, List<ConfigKey> pConfigParameters)
        {
            Positions = new List<AccountPosition>();

            AccountToSync = new Account();
        }

        #endregion

        #region IAccountReferenceHandler Methods

        public bool SyncAccountPositions(InstructionBasedRouting.BusinessEntities.Account account)
        {
            AbortOnTimeout = false;
            return true;
        }

        public bool SyncAccountBalance(InstructionBasedRouting.BusinessEntities.Account account)
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

        public InstructionBasedRouting.BusinessEntities.Account GetAccountToSync()
        {
            return AccountToSync;
        }

        public List<InstructionBasedRouting.BusinessEntities.AccountPosition> GetActivePositions()
        {
            return Positions;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
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
            Logger = OnLogMsg;
            Account = new Account() { Name = "Primary Account Manager Test" };
        }

        #endregion

        #region Protected Attributes

        public Account Account { get; set; }

        protected OnLogMessage Logger { get; set; }

        #endregion

        #region Public Methods
        public bool SyncAccountPositions(Account account)
        {
            return true;
        }

        public bool SyncAccountBalance(InstructionBasedRouting.BusinessEntities.Account account)
        {
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
            return false;
        }

        public Account GetAccountToSync()
        {
            return Account;
        }

        public List<AccountPosition> GetActivePositions()
        {
            return new List<AccountPosition>();
        }

        #endregion
    }
}

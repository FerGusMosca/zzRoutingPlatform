using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces
{
    public interface IAccountReferenceHandler
    {
        #region Public Methods

        bool SyncAccountPositions(Account account);

        bool SyncAccountBalance(Account account);

        Boolean ReadyAccountSummary();

        Boolean WaitingAccountPositions();

        bool IsAbortOnTimeout();

        Account GetAccountToSync();

        List<AccountPosition> GetActivePositions();

        #endregion
    }
}

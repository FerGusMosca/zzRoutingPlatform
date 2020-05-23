using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.IBR.Bitfinex.DAL
{
    public class BitfinexAccountManager : IAccountReferenceHandler
    {
        #region Protected Attributes

        protected bool ReqAccountSummary { get; set; }

        protected bool ReqAccountPositions { get; set; }

        protected bool AbortOnTimeout { get; set; }

        #endregion


        #region Public Methods

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
            throw new NotImplementedException();
        }

        public List<AccountPosition> GetActivePositions()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

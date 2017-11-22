using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces
{
    public interface IAccountManagerAccessLayer
    {
        Account GetById(int id);

        void Persist(Account account);
    }
}

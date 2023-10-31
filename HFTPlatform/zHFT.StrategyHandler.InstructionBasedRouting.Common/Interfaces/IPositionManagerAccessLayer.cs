using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces
{
    public interface IPositionManagerAccessLayer
    {
        AccountPosition GetActivePositionBySymbol(string symbol, int accountId);

        AccountPosition GetById(long id);

        //void DeleteAllOnline(int accountId);

        //void Persist(AccountPosition pos);

        void PersistAndReplace(List<AccountPosition> positions, int accountId);

        //void Delete(AccountPosition pos);
    }
}

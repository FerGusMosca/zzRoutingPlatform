using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces
{
    public interface IInstructionManagerAccessLayer
    {
        Instruction GetById(long instrId);

        List<Instruction> GetPendingInstructions(long accountNumber);

        List<Instruction> GetRelatedInstructions(long accountNumber, int idSyncInstr, InstructionType type);

        void Persist(Instruction instr);
    }
}

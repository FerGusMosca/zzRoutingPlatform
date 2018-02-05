using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;

namespace zHFT.StrategyHandler.InstructionBasedRouting.Common.DTO
{
    public class IcebergPositionDTO
    {
        #region Public Attributes

        public Instruction Instruction { get; set; }

        public List<Position> Positions { get; set; }

        public double TotalAmmount { get; set; }

        public double CumAmmount { get; set; }

        public double LeavesAmmount { get; set; }

        public zHFT.Main.Common.Enums.PositionStatus PositionStatus { get; set; }

        public zHFT.Main.Common.Enums.Side Side { get; set; }

        public double CurrentStepAmmount { get; set; }

        public int CurrentStep { get; set; }


        #endregion
    }
}

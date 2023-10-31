using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities
{
    public class InstructionType
    {
        #region Public Consts

        public static string _NEW_POSITION = "NEW_POS";
        public static string _UNWIND_POSITION = "UWND_POS";
        public static string _SYNC_BALANCE = "SYNC_BAL";
        public static string _SYNC_POSITIONS = "SYNC_POS";
        public static string _CANCEL_POSITIONS = "CANCEL_POS";
        public static string _POST_SYNC_POSITIONS = "P_SYNC_POS";
        public static string _CLEAN_ALL_POS = "CL_ALL_POS";

        #endregion

        #region Public Attributes

        public string Type { get; set; }

        public string Description { get; set; }

        #endregion

        #region Public Methods

        public static InstructionType GetSyncAccountInstr()
        {
            return new InstructionType() { Type = _SYNC_BALANCE };
        }


        public static InstructionType GetNewPosInstr()
        {
            return new InstructionType() { Type = _NEW_POSITION };
        }

        public static InstructionType GetUnwindPos()
        {
            return new InstructionType() { Type = _UNWIND_POSITION };
        }

        public static InstructionType GetSyncPositionsInstr()
        {
            return new InstructionType() { Type = _SYNC_POSITIONS };
        }

        public static InstructionType GetPostSyncPositionsInstr()
        {
            return new InstructionType() { Type = _POST_SYNC_POSITIONS };
        }

        #endregion
    }
}

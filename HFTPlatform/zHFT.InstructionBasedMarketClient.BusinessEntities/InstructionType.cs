using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BusinessEntities
{
    public class InstructionType
    {
        #region Public Consts

        public static string _NEW_POSITION = "NEW_POS";
        public static string _UNWIND_POSITION = "UWND_POS";
        public static string _SYNC_BALANCE = "SYNC_BAL";
        public static string _SYNC_POSITIONS = "SYNC_POS";

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

        public static InstructionType GetSyncPositionsInstr()
        {
            return new InstructionType() { Type = _SYNC_POSITIONS };
        }

        #endregion
    }
}

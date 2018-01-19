using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class CancelAllPositionsWrapper : Wrapper
    {
        #region Constructors

        public CancelAllPositionsWrapper(IConfiguration pConfig)
        {
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return "";
        }

        public override Actions GetAction()
        {
            return Actions.CANCEL_ALL_POSITIONS;
        }


        public override object GetField(Fields field)
        {
            return PositionFields.NULL;
        }

        #endregion
    }
}

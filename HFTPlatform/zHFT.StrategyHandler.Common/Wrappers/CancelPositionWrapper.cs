using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class CancelPositionWrapper:PositionWrapper
    {
         #region Constructors

        public CancelPositionWrapper(Position pPosition, IConfiguration pConfig) :base(pPosition,pConfig)
        {
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Position != null)
            {
                return "";//TO DO : Desarrollar el método to string
            }
            else
                return "";
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.CANCEL_POSITION;
        }

        #endregion
    }
}

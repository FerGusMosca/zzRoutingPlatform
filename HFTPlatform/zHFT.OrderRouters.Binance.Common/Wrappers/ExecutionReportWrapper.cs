using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Binance.Common.Wrappers
{
    public class ExecutionReportWrapper : Wrapper
    {
        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Common.Wrappers
{
    public class OrderListWrapper:Wrapper
    {
        public override object GetField(Main.Common.Enums.Fields field)
        {
            return null;
        }

        public override Actions GetAction()
        {
            return Actions.ORDER_LIST;
        }
    }
}

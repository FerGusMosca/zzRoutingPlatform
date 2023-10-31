using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;

namespace zHFT.OrderRouters.Common.Wrappers
{
    public class CancelOrderWrapper:OrderWrapper
    {
        #region Constructors

        public CancelOrderWrapper(Order pOrder, IConfiguration pConfig) 
        {
            Order = pOrder;

            Config = pConfig;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Order != null)
            {
                return "";//TO DO : Desarrollar el método to string
            }
            else
                return "";
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.CANCEL_ORDER;
        }

        #endregion
    }
}

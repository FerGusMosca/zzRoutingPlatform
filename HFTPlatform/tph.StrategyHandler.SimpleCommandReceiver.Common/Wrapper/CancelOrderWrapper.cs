using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.OrderRouters.Common.Wrappers;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper
{
    public class CancelOrderWrapper:OrderWrapper
    {
        #region Constructors

        public CancelOrderWrapper(Order pOrder) 
        {
            Order = pOrder;

           
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

        public override Actions GetAction()
        {
            return Actions.CANCEL_ORDER;
        }

        #endregion
    }
}
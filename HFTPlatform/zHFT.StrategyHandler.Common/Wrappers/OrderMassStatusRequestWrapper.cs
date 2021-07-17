using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.StrategyHandler.Common.Wrappers
{
    public class OrderMassStatusRequestWrapper:Wrapper
    {
        public override object GetField(Fields field)
        {
            return OrderFields.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.ORDER_MASS_STATUS_REQUEST;
        }
    }
}
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.Wrapper
{
    public class CancelAllWrapper:zHFT.Main.Common.Wrappers.Wrapper
    {
        public override object GetField(Fields field)
        {
            return OrderFields.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.CANCEL_ALL_POSITIONS;
        }
    }
}
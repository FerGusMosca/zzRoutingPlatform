using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Common.Wrappers
{
    public class OrderCancelRejectWrapper: Wrapper
    {
        #region Private Attributes

        protected string ClOrdId { get; set; }

        protected string OrderId { get; set; }

        protected string Reason { get; set; }

        protected CxlRejReason CxlRejReason { get; set; }

        protected CxlRejResponseTo CxlRejResponseTo { get; set; }

        #endregion


        #region Constructors


        public OrderCancelRejectWrapper(string pClOrdId, string pOrderId, string pReason,
                                        CxlRejReason pCxlRejReason, CxlRejResponseTo pCxlRejResponseTo)
        {
            ClOrdId = pClOrdId;
            OrderId = pOrderId;
            Reason = pReason;
            CxlRejReason = pCxlRejReason;
            CxlRejResponseTo = CxlRejResponseTo;

        }

        #endregion
        
        public override object GetField(Fields field)
        {
            OrderCancelRejectField ocrField = (OrderCancelRejectField)field;


            if (ocrField == OrderCancelRejectField.OrderID)
                return OrderId;
            else if (ocrField == OrderCancelRejectField.ClOrdID)
                return ClOrdId;
            else if (ocrField == OrderCancelRejectField.OrigClOrdID)
                return null;
            else if (ocrField == OrderCancelRejectField.Text)
                return Reason;
            else if (ocrField == OrderCancelRejectField.OrdStatus)
                return OrdStatus.Unkwnown;
            else if (ocrField == OrderCancelRejectField.CxlRejResponseTo)
                return CxlRejResponseTo;
            else if (ocrField == OrderCancelRejectField.CxlRejReason)
                return CxlRejReason;
            else
                return OrderCancelRejectField.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.ORDER_CANCEL_REJECT;
        }
    }
}
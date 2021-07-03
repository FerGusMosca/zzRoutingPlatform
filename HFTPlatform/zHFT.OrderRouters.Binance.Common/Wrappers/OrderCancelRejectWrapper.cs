using System;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Binance.Common.Wrappers
{
    public class OrderCancelRejectWrapper:Wrapper
    {
        #region Private Attributes

        protected string OrigClOrdId { get; set; }
        
        protected string ClOrdId { get; set; }
        
        protected string Text { get; set; }

        #endregion

        #region Constructors

        public OrderCancelRejectWrapper(string pOrigClOrdId,string pClOrdId, string pText)
        {
            OrigClOrdId = pOrigClOrdId;
            ClOrdId = pClOrdId;
            Text = pText;
        }

        #endregion

        #region Protected Methods


        #endregion

        #region Public Methods

        public override object GetField(Fields field)
        {
            OrderCancelRejectField ocrField = (OrderCancelRejectField)field;

            if (ocrField == OrderCancelRejectField.OrderID)
                return OrderCancelRejectField.NULL;
            else if (ocrField == OrderCancelRejectField.ClOrdID)
                return ClOrdId;
            else if (ocrField == OrderCancelRejectField.OrigClOrdID)
                return OrigClOrdId;
            else if (ocrField == OrderCancelRejectField.Text)
                return Text;
            else if (ocrField == OrderCancelRejectField.OrdStatus)
                return OrderCancelRejectField.NULL;
            else if (ocrField == OrderCancelRejectField.CxlRejResponseTo)
                return CxlRejResponseTo.OrderCancelRequest;
            else if (ocrField == OrderCancelRejectField.CxlRejReason)
                return CxlRejReason.Other;
            else
                return OrderCancelRejectField.NULL; 
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.ORDER_CANCEL_REJECT;
        }

        #endregion
    }
}
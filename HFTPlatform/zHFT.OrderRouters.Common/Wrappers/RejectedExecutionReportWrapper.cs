using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Common.Wrappers
{
    public class RejectedExecutionReportWrapper : Wrapper
    {
       
        #region Private Attributes

        protected Order Order { get; set; }

        protected string Text { get; set; }

        #endregion

        #region Constructors

        public RejectedExecutionReportWrapper(Order pOrder,string text)
        {
            Order = pOrder;

            Text = text;
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.ExecType)
                return ExecType.Rejected;
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return OrdStatus.Rejected;
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return 0;
            else if (xrField == ExecutionReportFields.CumQty)
                return 0;
            else if (xrField == ExecutionReportFields.AvgPx)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Commission)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Text)
                return Text;
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LastPx)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LastMkt)
                return ExecutionReportFields.NULL;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return Order.OrderId;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return Order.ClOrdId;
            else if (xrField == ExecutionReportFields.Symbol)
                return Order.Security.Symbol;
            else if (xrField == ExecutionReportFields.OrderQty)
                return Order.OrderQty;
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdType)
                return Order.OrdType;
            else if (xrField == ExecutionReportFields.Price)
                return Order.Price;
            else if (xrField == ExecutionReportFields.StopPx)
                return Order.StopPx;
            else if (xrField == ExecutionReportFields.Currency)
                return Order.Currency;
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return Order.MinQty != int.MaxValue ? (int?)Order.MinQty : null;
            else if (xrField == ExecutionReportFields.Side)
                return Order.Side;
            else if (xrField == ExecutionReportFields.QuantityType)
                return Order.QuantityType;
            else if (xrField == ExecutionReportFields.PriceType)
                return Order.PriceType;
            return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        #endregion
    }
}

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
    public class GenericExecutionReportWrapper : Wrapper
    {
        #region Private Attributes

        protected ExecutionReport ExecutionReport { get; set; }


        #endregion

        #region Constructors

        public GenericExecutionReportWrapper(ExecutionReport pExecReport)
        {
            ExecutionReport = pExecReport;

        }

        #endregion

        #region Protected Methods

        #endregion

        #region Public Methods


        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (ExecutionReport == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.ExecType)
                return ExecutionReport.ExecType;
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReport.ExecID;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return ExecutionReport.OrdStatus;
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReport.OrdRejReason;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return ExecutionReport.LeavesQty;
            else if (xrField == ExecutionReportFields.CumQty)
                return ExecutionReport.CumQty;
            else if (xrField == ExecutionReportFields.AvgPx)
                return ExecutionReport.AvgPx;
            else if (xrField == ExecutionReportFields.Commission)
                return ExecutionReport.Commission;
            else if (xrField == ExecutionReportFields.Text)
                return ExecutionReport.Text;
            else if (xrField == ExecutionReportFields.TransactTime)
                return ExecutionReport.TransactTime;
            else if (xrField == ExecutionReportFields.LastQty)
                return ExecutionReport.LastQty;
            else if (xrField == ExecutionReportFields.LastPx)
                return ExecutionReport.LastPx;
            else if (xrField == ExecutionReportFields.LastMkt)
                return ExecutionReport.LastMkt;

            if (ExecutionReport.Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return ExecutionReport.Order.OrderId;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return ExecutionReport.Order.ClOrdId;
            else if (xrField == ExecutionReportFields.Symbol)
                return ExecutionReport.Order.Security.Symbol;
            else if (xrField == ExecutionReportFields.OrderQty)
                return ExecutionReport.Order.OrderQty;
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdType)
                return ExecutionReport.Order.OrdType;
            else if (xrField == ExecutionReportFields.Price)
                return ExecutionReport.Order.Price;
            else if (xrField == ExecutionReportFields.StopPx)
                return ExecutionReport.Order.StopPx;
            else if (xrField == ExecutionReportFields.Currency)
                return ExecutionReport.Order.Currency;
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return ExecutionReport.Order.MinQty != int.MaxValue ? (int?)ExecutionReport.Order.MinQty : null;
            else if (xrField == ExecutionReportFields.Side)
                return ExecutionReport.Order.Side;
            else if (xrField == ExecutionReportFields.QuantityType)
                return ExecutionReport.Order.QuantityType;
            else if (xrField == ExecutionReportFields.PriceType)
                return ExecutionReport.Order.PriceType;
            return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        #endregion
    }
}

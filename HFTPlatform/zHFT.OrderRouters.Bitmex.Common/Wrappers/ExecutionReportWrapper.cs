using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bitmex.BusinessEntities;


namespace zHFT.OrderRouters.Bitmex.Common.Wrappers
{
    public class ExecutionReportWrapper : BaseExecutionReportWrapper
    {
        #region Constructors

        public ExecutionReportWrapper(ExecutionReport pExecutionReport, zHFT.OrderRouters.Bitmex.BusinessEntities.Order pOrder)
        {
            ExecutionReport = pExecutionReport;
            Order = pOrder;
        }

        #endregion

        #region Protected Attributes

        protected ExecutionReport ExecutionReport { get; set; }

        protected zHFT.OrderRouters.Bitmex.BusinessEntities.Order Order { get; set; }

        #endregion

       

        #region Overriden Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (ExecutionReport == null)
                return ExecutionReportFields.NULL;


            if (xrField == ExecutionReportFields.ExecType)
                return GetExecTypeFromBitMexStatus(ExecutionReport.ExecType);
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReport.ExecID;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return GetOrdStatusFromBitMexStatus(ExecutionReport.OrdStatus);
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return GetOrdRejReasonFromBitMexStatus(ExecutionReport.OrdRejReason);
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
                return ExecutionReport.UnderlyingLastPx;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return Order.OrderId;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return Order.ClOrdId;
            else if (xrField == ExecutionReportFields.Symbol)
                return Order.SymbolPair;
            else if (xrField == ExecutionReportFields.OrderQty)
                return Order.OrderQty;
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdType)
                return Order.OrdType;
            else if (xrField == ExecutionReportFields.Price)
                return Order.Price;
            else if (xrField == ExecutionReportFields.StopPx)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Currency)
                return Order.Currency;
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Side)
                return Order.Side;
            else if (xrField == ExecutionReportFields.QuantityType)
                return QuantityType.CURRENCY;//In IB v1.0 we only work with SHARE orders
            else if (xrField == ExecutionReportFields.PriceType)
                return PriceType.FixedAmount;//In IB v1.0 we only work with FIXED AMMOUNT orders

            return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        #endregion
    }
}

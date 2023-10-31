using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouter.Mock.Common.Wrappers
{
    public class ExecutionReportWrapper:Wrapper
    {
        #region Constructors

        public ExecutionReportWrapper(ExecType pExecType, OrdStatus pOrdStatus, double pLeavesQty, double pCumQty,
                                      double? pAvgPx, double? pLastPx,double? pLastQty, Order pOrder, string text="")
        {
            ExecType = pExecType;
            OrdStatus = pOrdStatus;
            LeavesQty = pLeavesQty;
            CumQty = pCumQty;
            AvgPx = pAvgPx;
            LastPx = pLastPx;
            Order = pOrder;
            Text = text;
            LastQty = pLastQty;
        
        }

        #endregion

        #region Public Attributes

        public ExecType ExecType { get; set; }

        public OrdStatus OrdStatus { get; set; }

        public double LeavesQty { get; set; }

        public double CumQty { get; set; }

        public double? AvgPx { get; set; }

        public double? LastPx { get; set; }

        public double? LastQty { get; set; }

        public Order Order { get; set; }

        public string Text { get; set; }

        #endregion


        #region Overriden Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;


            if (xrField == ExecutionReportFields.ExecType)
                return ExecType;
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return OrdStatus;
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return LeavesQty;
            else if (xrField == ExecutionReportFields.CumQty)
                return CumQty;
            else if (xrField == ExecutionReportFields.AvgPx)
                return AvgPx;
            else if (xrField == ExecutionReportFields.Commission)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Text)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return LastQty;
            else if (xrField == ExecutionReportFields.LastPx)
                return LastPx;
            else if (xrField == ExecutionReportFields.LastMkt)
                return ExecutionReportFields.NULL;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return Order.OrderId;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return Order.ClOrdId;
            else if (xrField == ExecutionReportFields.Symbol)
                return Order.Symbol;
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
                return QuantityType.SHARES;//In IB v1.0 we only work with SHARE orders
            else if (xrField == ExecutionReportFields.PriceType)
                return PriceType.FixedAmount;//In IB v1.0 we only work with FIXED AMMOUNT orders
            else
                return ExecutionReportFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        #endregion
    }
}

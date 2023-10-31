using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace tph.StrategyHandler.SimpleCommandSender.Common.Wrappers
{
    public class ExecutionReportWrapper:Wrapper
    {
        
        #region Constructors

        public ExecutionReportWrapper(ExecutionReportDTO pExecReport,NewOrderReq pOrder)
        {
            ExecutionReport = pExecReport;
            Order = pOrder;
        }

        #endregion

        #region  Protected Attributes
        
        protected ExecutionReportDTO ExecutionReport { get; set; }
        
        protected  NewOrderReq Order { get; set; }

        #endregion
        
        
        public override object GetField(Fields field)
        {
               ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (ExecutionReport == null)
                return ExecutionReportFields.NULL;


            if (xrField == ExecutionReportFields.ExecType)
                return ExecutionReport.ExecType;
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return ExecutionReport.OrdStatus;
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
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
                return ExecutionReportFields.NULL;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return ExecutionReport.OrderId;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return ExecutionReport.ClOrdId;
            else if (xrField == ExecutionReportFields.Symbol)
                return Order.Symbol;
            else if (xrField == ExecutionReportFields.OrderQty)
                return Order.Qty;
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdType)
                return Order.GetOrdType();
            else if (xrField == ExecutionReportFields.Price)
                return Order.Price;
            else if (xrField == ExecutionReportFields.StopPx)
                return  null;
            else if (xrField == ExecutionReportFields.Currency)
                return Order.Currency;
            else if (xrField == ExecutionReportFields.TimeInForce)
                return Order.GetTimeInforce();
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return  null;
            else if (xrField == ExecutionReportFields.Side)
                return Order.GetSide();
            else if (xrField == ExecutionReportFields.QuantityType)
                return QuantityType.SHARES;//In IB v1.0 we only work with SHARE orders
            else if (xrField == ExecutionReportFields.PriceType)
                return PriceType.FixedAmount;//In IB v1.0 we only work with FIXED AMMOUNT orders


            return ExecutionReportFields.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }
    }
}
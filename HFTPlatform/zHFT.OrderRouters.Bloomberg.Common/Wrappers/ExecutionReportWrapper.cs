using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bloomberg.Common.DTOs;

namespace zHFT.OrderRouters.Bloomberg.Common.Wrappers
{
    public class ExecutionReportWrapper : Wrapper
    {
        #region Private Attributes

        protected IConfiguration Config { get; set; }

        protected OrderDTO Order { get; set; }

        protected OrdStatus OrdStatus { get; set; }

        protected ExecType ExecType { get; set; }

        protected string RejReason { get; set; }

        #endregion

        #region Constructors

        public ExecutionReportWrapper(OrderDTO pOrder, OrdStatus pOrdStatus, ExecType pExecType,string pRejReason, IConfiguration pConfig)
        {
            Order = pOrder;
            OrdStatus = pOrdStatus;
            ExecType = pExecType;
            RejReason = pRejReason;
            Config = pConfig;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format("Execution Report for symbol {2}: Order Status={0} - Exec Type={1} {3}",
                OrdStatus.ToString(), ExecType.ToString(), Order.Ticker, RejReason != null ? string.Format("Rej. Reason:{0}", RejReason) : "");
        }

        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (Order == null)
                return ExecutionReportFields.NULL;


            if (xrField == ExecutionReportFields.ExecType)
                return ExecType;
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return OrdStatus;
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return 0;
            else if (xrField == ExecutionReportFields.CumQty)
                return 0;
            else if (xrField == ExecutionReportFields.AvgPx)
                return 0;
            else if (xrField == ExecutionReportFields.Commission)
                return 0;
            else if (xrField == ExecutionReportFields.Text)
                return RejReason;
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LastPx)
                return 0;
            else if (xrField == ExecutionReportFields.LastMkt)
                return ExecutionReportFields.NULL;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return Order.OrderId;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return Order.ClOrderId;
            else if (xrField == ExecutionReportFields.Symbol)
                return Order.Ticker;
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
                return null;
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Side)
                return Order.Side;
            else if (xrField == ExecutionReportFields.QuantityType)
                return QuantityType.SHARES;//In IB v1.0 we only work with SHARE orders
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Binance.Common.DTO;

namespace zHFT.OrderRouters.Binance.Common.Wrappers
{
    public class ExecutionReportWrapper : Wrapper
    {
        #region Protected Attributes

        protected Order Order { get; set; }

        protected ExecutionReportDTO ExecutionReport { get; set; }

        #endregion


        #region Private Consts

        private string _FILLED = "FILLED";
        private string _PARTIALLY_FILLED = "PARTIALLY_FILLED";
        private string _NEW = "NEW";
        private string _CANCELED = "CANCELED";
        private string _PENDING_CANCEL = "PENDING_CANCEL";
        private string _REJECTED = "REJECTED";
        private string _EXPIRED = "EXPIRED";

        #endregion

        #region Constructors

        public ExecutionReportWrapper(Order pOrder, ExecutionReportDTO pExecReportDTO)
        {
            Order = pOrder;

            ExecutionReport = pExecReportDTO;
        }

        #endregion

        #region Protected Methods

        protected ExecType GetExecTypeFromBinanceStatus()
        {
            if (ExecutionReport.Status == _FILLED)
                return ExecType.Trade;
            else if (ExecutionReport.Status == _PARTIALLY_FILLED)
                return ExecType.Trade;
            else if (ExecutionReport.Status == _NEW)
                return ExecType.New;
            else if (ExecutionReport.Status == _CANCELED)
                return ExecType.Canceled;
            else if (ExecutionReport.Status == _PENDING_CANCEL)
                return ExecType.PendingCancel;
            else if (ExecutionReport.Status == _REJECTED)
                return ExecType.Rejected;
            else if (ExecutionReport.Status == _EXPIRED)
                return ExecType.Expired;
            else
                return ExecType.Unknown;

        }

        protected OrdStatus GetOrdStatusFromBinanceStatus()
        {
            if (ExecutionReport.Status == _FILLED)
                return OrdStatus.Filled;
            else if (ExecutionReport.Status == _PARTIALLY_FILLED)
                return OrdStatus.PartiallyFilled;
            else if (ExecutionReport.Status == _NEW)
                return OrdStatus.New;
            else if (ExecutionReport.Status == _CANCELED)
                return OrdStatus.Canceled;
            else if (ExecutionReport.Status == _PENDING_CANCEL)
                return OrdStatus.PendingCancel;
            else if (ExecutionReport.Status == _REJECTED)
                return OrdStatus.Rejected;
            else if (ExecutionReport.Status == _EXPIRED)
                return OrdStatus.Expired;
            else
                return OrdStatus.Suspended;
        }

        protected decimal ProcessLeavesQty()
        {

            if (ExecutionReport.Status == _FILLED)
                return 0;
            else if (ExecutionReport.Status == _PARTIALLY_FILLED)
                return ExecutionReport.LeavesQty;
            else if (ExecutionReport.Status == _NEW)
                return ExecutionReport.LeavesQty;
            else if (ExecutionReport.Status == _CANCELED)
                return 0;
            else if (ExecutionReport.Status == _PENDING_CANCEL)
                return ExecutionReport.LeavesQty;
            else if (ExecutionReport.Status == _REJECTED)
                return 0;
            else if (ExecutionReport.Status == _EXPIRED)
                return 0;
            else
                return 0;
        
        }

        #endregion

        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (Order == null)
                return ExecutionReportFields.NULL;


            if (xrField == ExecutionReportFields.ExecType)
                return GetExecTypeFromBinanceStatus();
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return GetOrdStatusFromBinanceStatus();
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return ProcessLeavesQty();
            else if (xrField == ExecutionReportFields.CumQty)
                return ExecutionReport.ExecutedQty;
            else if (xrField == ExecutionReportFields.AvgPx)
                return 0;
            else if (xrField == ExecutionReportFields.Commission)
                return 0;
            else if (xrField == ExecutionReportFields.Text)
                return ExecutionReport.Text;
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LastPx)
                return Order.Price;
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
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Currency)
                return Order.Currency;
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return Order.MinQty != int.MaxValue ? (int?)Order.MinQty : null;
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


        public override string ToString()
        {
            OrdStatus ordStatus = GetOrdStatusFromBinanceStatus();
            ExecType? execType = GetExecTypeFromBinanceStatus();

            return string.Format("Execution Report for symbol {2}: Order Status={0} - Exec Type={1}",
                                            ordStatus.ToString(), execType.ToString(), Order.Symbol);
        }

        #endregion
    }
}

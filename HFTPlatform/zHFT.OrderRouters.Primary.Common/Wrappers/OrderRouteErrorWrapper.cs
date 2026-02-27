using System;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using ExecType = zHFT.Main.Common.Enums.ExecType;
using OrdStatus = zHFT.Main.Common.Enums.OrdStatus;

namespace zHFT.OrderRouters.Primary.Common.Wrappers
{
    /// <summary>
    /// Synthetic wrapper that represents a routing failure at the order level.
    /// Built locally when an exception occurs in RouteNewOrder, so the upstream
    /// consumer receives a well-formed rejection instead of silence.
    /// </summary>
    public class OrderRouteErrorWrapper : Wrapper
    {
        #region Private Attributes

        private readonly string _clOrdId;
        private readonly string _symbol;
        private readonly string _errorText;
        private readonly OrdStatus _ordStatus;
        private readonly ExecType _execType;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a synthetic rejection wrapper from a routing exception.
        /// </summary>
        /// <param name="clOrdId">ClOrdID of the order that failed to route</param>
        /// <param name="symbol">Symbol of the order</param>
        /// <param name="errorText">Exception message to propagate as rejection reason</param>
        public OrderRouteErrorWrapper(string clOrdId, string symbol, string errorText)
        {
            _clOrdId = clOrdId;
            _symbol = symbol;
            _errorText = errorText;
            _ordStatus = OrdStatus.Rejected;
            _execType = ExecType.Rejected;
        }

        #endregion

        #region Public Overrides

        public override Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        public override object GetField(Fields field)
        {
            ExecutionReportFields erField = (ExecutionReportFields)field;

            if (erField == ExecutionReportFields.ClOrdID)
                return _clOrdId;

            if (erField == ExecutionReportFields.Symbol)
                return _symbol;

            if (erField == ExecutionReportFields.OrdStatus)
                return _ordStatus;

            if (erField == ExecutionReportFields.ExecType)
                return _execType;

            if (erField == ExecutionReportFields.Text)
                return _errorText;

            if (erField == ExecutionReportFields.CumQty)
                return 0d;

            if (erField == ExecutionReportFields.LeavesQty)
                return 0d;

            if (erField == ExecutionReportFields.OrderID)
                return "NONE";

            return null;
        }

        #endregion
    }
}
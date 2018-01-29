using Bittrex;
using Bittrex.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace zHFT.OrderRouters.Bittrex.Common.Wrappers
{
    public class ExecutionReportWrapper:Wrapper
    {
        #region Protected Attributes

        public Order Order { get; set; }

        public GetOrderResponse OpenOrder { get; set; }
       

        #endregion

       

        #region Constructors

        public ExecutionReportWrapper(Order pOrder, GetOrderResponse pOpenOrder)
        {
            Order = pOrder;

            OpenOrder = pOpenOrder;
        }

        #endregion

        #region Protected Methods

        protected ExecType GetExecTypeFromBittrexStatus()
        {
            if (OpenOrder == null)
                return ExecType.Canceled;

            if (OpenOrder.QuantityRemaining == 0)
                return ExecType.Trade;
            else if (OpenOrder.QuantityRemaining < OpenOrder.Quantity)
                return ExecType.Trade;
            else if (OpenOrder.CancelInitiated)
                return ExecType.Canceled;
            else if (OpenOrder.QuantityRemaining == OpenOrder.Quantity)
                return ExecType.New;
            else
                return ExecType.Unknown;
        }

        protected OrdStatus GetOrdStatusFromBittrexStatus()
        {
            if (OpenOrder == null)
                return OrdStatus.Canceled; ;

            if (OpenOrder.QuantityRemaining == 0)
                return OrdStatus.Filled;
            else if (OpenOrder.QuantityRemaining > 0 && OpenOrder.QuantityRemaining < OpenOrder.Quantity)
                return OrdStatus.PartiallyFilled;
            else if (OpenOrder.CancelInitiated)
                return OrdStatus.Canceled;
            else if (OpenOrder.QuantityRemaining == OpenOrder.Quantity)
                return OrdStatus.New;
            else if (OpenOrder.QuantityRemaining==0)
                return OrdStatus.Filled;
            else
                return OrdStatus.Suspended;
        }

        protected OrdType? GetOrdTypeFromBittrexOrdType()
        {
            return Order.OrdType;
        }

        protected Side GetSideFromBittrexSide()
        {
            return Order.Side;
        }

        #endregion


        #region Public Methods

        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (Order == null)
                return ExecutionReportFields.NULL;


            if (xrField == ExecutionReportFields.ExecType)
                return GetExecTypeFromBittrexStatus();
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return GetOrdStatusFromBittrexStatus();
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return  OpenOrder.QuantityRemaining ;
            else if (xrField == ExecutionReportFields.CumQty)
                return OpenOrder.Quantity - OpenOrder.QuantityRemaining;
            else if (xrField == ExecutionReportFields.AvgPx)
                return 0;
            else if (xrField == ExecutionReportFields.Commission)
                return OpenOrder.CommissionPaid;
            else if (xrField == ExecutionReportFields.Text)
                return Order.RejReason;
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
                return OpenOrder.OrderUuid;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return Order.ClOrdId;
            else if (xrField == ExecutionReportFields.Symbol)
                return Order.Symbol;
            else if (xrField == ExecutionReportFields.OrderQty)
                return Order.OrderQty;
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdType)
                return GetOrdTypeFromBittrexOrdType();
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
                return GetSideFromBittrexSide();
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
            OrdStatus ordStatus = GetOrdStatusFromBittrexStatus();
            ExecType? execType = GetExecTypeFromBittrexStatus();

            return string.Format("Execution Report for symbol {2}: Order Status={0} - Exec Type={1}",
                                 ordStatus.ToString(), execType.ToString(), Order.Symbol);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.Common;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.InvertirOnline.Common.DTO;

namespace zHFT.OrderRouters.InvertirOnline.Common.Wrappers
{
    public class RejectedExecutionReportWrapper:Wrapper
    {

        #region Private Attributes

        protected string Text { get; set; }

        protected Order Order { get; set; }

        #endregion

        #region Constructors

        public RejectedExecutionReportWrapper(string pText, Order pOrder)
        {
            Text = pText;

            Order = pOrder;
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Order != null)
            {
                return string.Format("Symbol {0} ExecType {1} OrdStatus {2}", Order.simbolo, ExecType.Rejected, OrdStatus.Rejected);
            }
            else
                return "";
        }


        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;


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
                return Order.cantidad;
            else if (xrField == ExecutionReportFields.AvgPx)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Commission)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Text)
                return ExecutionReportFields.NULL;
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
                return Order.simbolo + "." + ExchangeConverter.GetInstrMarketFromIolMarket(Order.mercado);
            else if (xrField == ExecutionReportFields.OrderQty)
                return Order.cantidad;
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdType)
                return Order.ordtype;
            else if (xrField == ExecutionReportFields.Price)
                return Order.precio;
            else if (xrField == ExecutionReportFields.StopPx)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Currency)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Side)
                return Order.side;
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

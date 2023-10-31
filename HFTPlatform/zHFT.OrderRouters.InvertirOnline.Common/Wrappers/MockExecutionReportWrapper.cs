using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.Common;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.InvertirOnline.Common.DTO;
using zHFT.OrderRouters.InvertirOnline.Common.Responses;

namespace zHFT.OrderRouters.InvertirOnline.Common.Wrappers
{
    public class MockExecutionReportWrapper : Wrapper
    {
        #region Private Static Consts


        #endregion

        #region Private Attributes

        protected NewOrderResponse NewOrderResponse { get; set; }

        protected Order Order { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion

        #region Constructors

        public MockExecutionReportWrapper(NewOrderResponse pNewOrderResponse, Order pOrder, IConfiguration pConfig)
        {
            NewOrderResponse = pNewOrderResponse;

            Order = pOrder;

            Config = pConfig;
        }

        #endregion

        #region Protected Methods

        protected OrdType? GetOrdTypeFromIOLOrdType()
        {
           return OrdType.Market;//Order Routing for IOL only Market for the moment
        }

        protected Side GetSideFromIOLSide()
        {
           return Order.side;
        }

        protected ExecType GetExecTypeFromIBStatus()
        {
            if (NewOrderResponse.IsOk)
                return ExecType.Trade;
            else
                return ExecType.Rejected;
        }

        protected OrdStatus GetOrdStatusFromIBStatus()
        {

            if (NewOrderResponse.IsOk)
                return OrdStatus.Filled;
            else
                return OrdStatus.Rejected;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Order != null && NewOrderResponse!=null)
            {
                return string.Format("Symbol {0} ExecType {1} OrdStatus {2}", Order.simbolo, GetExecTypeFromIBStatus(), GetOrdStatusFromIBStatus());
            }
            else
                return "";
        }


        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (NewOrderResponse == null || Order == null)
                return ExecutionReportFields.NULL;


            if (xrField == ExecutionReportFields.ExecType)
                return GetExecTypeFromIBStatus();
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return GetOrdStatusFromIBStatus();
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
                return GetOrdTypeFromIOLOrdType();
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
                return GetSideFromIOLSide();
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

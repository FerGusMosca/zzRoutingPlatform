using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.IB.Common.DTO;


namespace zHFT.OrderRouters.IB.Common.Wrappers
{
    public class ExecutionReportWrapper : Wrapper
    {
        #region Private Static Consts

        private static string _ORDER_TYPE_LIMIT = "LMT";
        private static string _ORDER_TYPE_MARKET = "KMT";

        private static string _SIDE_BUY = "BUY";
        private static string _SIDE_SELL = "SELL";

        #endregion

        #region Private Attributes

        protected OrderStatusDTO OrderStatusDTO { get; set; }

        protected IBApi.Order Order { get; set; }

        protected IBApi.Contract Contract { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion

        #region Constructors

        public ExecutionReportWrapper(OrderStatusDTO pOrderStatusDTO, IBApi.Order pOrder,IBApi.Contract pContract, IConfiguration pConfig)
        {
            OrderStatusDTO = pOrderStatusDTO;

            Order = pOrder;

            Contract = pContract;

            Config = pConfig;
        }

        #endregion

        #region Protected Methods

        protected OrdType? GetOrdTypeFromIBOrdType()
        {
            if (Order.OrderType == _ORDER_TYPE_LIMIT)
                return OrdType.Limit;
            else if (Order.OrderType == _ORDER_TYPE_MARKET)
                return OrdType.Market;
            else
                return null;
        
        
        }

        protected Side GetSideFromIBSide()
        {
            if (Order.Action == _SIDE_BUY)
                return Side.Buy;
            else if (Order.Action == _SIDE_SELL)
                return Side.Sell;
            else
                return Side.Unknown;
        
        }

        protected ExecType GetExecTypeFromIBStatus()
        {
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_CANCELED)
                return ExecType.Canceled;
            else if (OrderStatusDTO.Status.Contains(OrderStatusDTO._STATUS_FILLED) )
                return ExecType.Trade;
            else if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_INACTIVE)
                return ExecType.PendingNew;
            if (OrderStatusDTO.Status.Contains(OrderStatusDTO._STATUS_FILLED) )
                return ExecType.Trade;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_PENDING_CANCEL)
                return ExecType.PendingCancel;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_PENDING_SUBMIT)
                return ExecType.PendingNew;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_PRE_SUBMITTED)
                return ExecType.PendingNew;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_SUBMITTED)
                return ExecType.New;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_REJECTED)
                return ExecType.Rejected;
            else
                throw new Exception(string.Format("Unknow execution report status type {0} for symbol {1}", OrderStatusDTO.Status, Contract != null ? Contract.Symbol : "<no symbols>"));
        }

        protected OrdStatus GetOrdStatusFromIBStatus()
        {
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_CANCELED)
                return OrdStatus.Canceled;
            else if (OrderStatusDTO.Status.Contains(OrderStatusDTO._STATUS_FILLED) && OrderStatusDTO.Remaining==0)
                return OrdStatus.Filled;
            else if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_INACTIVE)
                return OrdStatus.PendingNew;
            if (OrderStatusDTO.Status.Contains(OrderStatusDTO._STATUS_FILLED) && OrderStatusDTO.Remaining>0)
                return OrdStatus.PartiallyFilled;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_PENDING_CANCEL)
                return OrdStatus.PendingCancel;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_PENDING_SUBMIT)
                return OrdStatus.PendingNew;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_PRE_SUBMITTED)
                return OrdStatus.PendingNew;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_SUBMITTED)
                return OrdStatus.New;
            if (OrderStatusDTO.Status == OrderStatusDTO._STATUS_REJECTED)
                return OrdStatus.Rejected;
            else
                throw new Exception(string.Format("Unknow order status type {0} for symbol {1}", OrderStatusDTO.Status, Contract != null ? Contract.Symbol : "<no symbols>"));
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (OrderStatusDTO != null)
            {
                return string.Format("Symbol {0} ExecType {1} OrdStatus {2}", Contract.Symbol, GetExecTypeFromIBStatus(), GetOrdStatusFromIBStatus());
            }
            else
                return "";
        }


        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (OrderStatusDTO == null)
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
                return OrderStatusDTO.Remaining;
            else if (xrField == ExecutionReportFields.CumQty)
                return OrderStatusDTO.Filled;
            else if (xrField == ExecutionReportFields.AvgPx)
                return OrderStatusDTO.AvgFillPrice;
            else if (xrField == ExecutionReportFields.Commission)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Text)
                return OrderStatusDTO.ErrorMsg;
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LastPx)
                return OrderStatusDTO.LastFillPrice;
            else if (xrField == ExecutionReportFields.LastMkt)
                return ExecutionReportFields.NULL;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return Order.OrderId;
            else if (xrField == ExecutionReportFields.ClOrdID)
                return OrderStatusDTO.ClOrdId;
            else if (xrField == ExecutionReportFields.Symbol)
                return Contract.Symbol;
            else if (xrField == ExecutionReportFields.OrderQty)
                return Order.TotalQuantity;
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdType)
                return GetOrdTypeFromIBOrdType();
            else if (xrField == ExecutionReportFields.Price)
                return Order.LmtPrice;
            else if (xrField == ExecutionReportFields.StopPx)
                return Order.TrailStopPrice != double.MaxValue ? (double?)Order.TrailStopPrice : null;
            else if (xrField == ExecutionReportFields.Currency)
                return Contract.Currency;
            else if (xrField == ExecutionReportFields.ExpireDate)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.MinQty)
                return Order.MinQty != int.MaxValue ? (int?)Order.MinQty : null;
            else if (xrField == ExecutionReportFields.Side)
                return GetSideFromIBSide();
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

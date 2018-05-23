using Bloomberglp.Blpapi;
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

        protected Message Message { get; set; }

        #endregion

        #region Private Static Consts

        private string _SENT_STATUS = "SENT";
        private string _WORKING_STATUS = "WORKING";
        private string _PARTFILL_STATUS = "PARTFILL";
        private string _FILLED_STATUS = "FILLED";
        private string _CANCEL_REQ_STATUS = "CXLREQ";
        private string _CANCEL_PEND_STATUS = "CXLPEN";
        private string _CANCEL_STATUS = "CANCEL";
        private string _MODIFY_REQ_STATUS = "CXLRPRQ";
        private string _MODIFY_PEND_REQ_STATUS = "REPPEN";
        private string _MODIFY_STATUS = "MODIFIED";
        private string _REJECTED_STATUS = "REJECTED";
        private string _COMPLETED_STATUS = "COMPLETED";
        private string _EXPIRED_STATUS = "EXPIRED";
        private string _ASSIGN_STATUS = "ASSIGN";

        #endregion

        #region Constructors

        public ExecutionReportWrapper(OrderDTO pOrder, Message pMessage, IConfiguration pConfig)
        {
            Order = pOrder;
            Message = pMessage;
            Config = pConfig;
        }

        #endregion

        #region Protected Methods

        public OrdStatus GetOrdStatus()
        {
            string EMSX_STATUS = Message.AsElement.GetElementAsString("EMSX_STATUS");

            if (EMSX_STATUS == _SENT_STATUS)
                return OrdStatus.PendingNew;
            else if (EMSX_STATUS == _WORKING_STATUS)
                return OrdStatus.New;
            else if (EMSX_STATUS == _PARTFILL_STATUS)
                return OrdStatus.PartiallyFilled;
            else if (EMSX_STATUS == _FILLED_STATUS)
                return OrdStatus.Filled;
            else if (EMSX_STATUS == _CANCEL_REQ_STATUS)
                return OrdStatus.PendingCancel;
            else if (EMSX_STATUS == _CANCEL_PEND_STATUS)
                return OrdStatus.PendingCancel;
            else if (EMSX_STATUS == _CANCEL_STATUS)
                return OrdStatus.Canceled;
            else if (EMSX_STATUS == _ASSIGN_STATUS)
                return OrdStatus.Canceled;
            else if (EMSX_STATUS == _MODIFY_REQ_STATUS)
                return OrdStatus.PendingReplace;
            else if (EMSX_STATUS == _MODIFY_PEND_REQ_STATUS)
                return OrdStatus.PendingReplace;
            else if (EMSX_STATUS ==_MODIFY_STATUS)
                return OrdStatus.Replaced;
            else if (EMSX_STATUS == _REJECTED_STATUS)
                return OrdStatus.Rejected;
            else if (EMSX_STATUS == _COMPLETED_STATUS)
                return OrdStatus.Filled;
            else if (EMSX_STATUS == _EXPIRED_STATUS)
                return OrdStatus.Expired;

            //En base a lo que hay en Message calcular el ORDSTATUS
            throw new Exception(string.Format("Unknown value por EMSX_STATUS:{0}", EMSX_STATUS));
        }

        public ExecType GetExecType()
        {
            string EMSX_STATUS = Message.AsElement.GetElementAsString("EMSX_STATUS");

            if (EMSX_STATUS == _SENT_STATUS)
                return ExecType.PendingNew;
            else if (EMSX_STATUS == _WORKING_STATUS)
                return ExecType.New;
            else if (EMSX_STATUS == _PARTFILL_STATUS)
                return ExecType.Trade;
            else if (EMSX_STATUS == _FILLED_STATUS)
                return ExecType.Trade;
            else if (EMSX_STATUS == _CANCEL_REQ_STATUS)
                return ExecType.PendingCancel;
            else if (EMSX_STATUS == _CANCEL_PEND_STATUS)
                return ExecType.PendingCancel;
            else if (EMSX_STATUS == _CANCEL_STATUS)
                return ExecType.Canceled;
            else if (EMSX_STATUS == _ASSIGN_STATUS)
                return ExecType.Canceled;
            else if (EMSX_STATUS == _MODIFY_REQ_STATUS)
                return ExecType.PendingReplace;
            else if (EMSX_STATUS == _MODIFY_PEND_REQ_STATUS)
                return ExecType.PendingReplace;
            else if (EMSX_STATUS == _MODIFY_STATUS)
                return ExecType.Replaced;
            else if (EMSX_STATUS == _REJECTED_STATUS)
                return ExecType.Rejected;
            else if (EMSX_STATUS == _COMPLETED_STATUS)
                return ExecType.Trade;
            else if (EMSX_STATUS == _EXPIRED_STATUS)
                return ExecType.Expired;

            //En base a lo que hay en Message calcular el ORDSTATUS
            throw new Exception(string.Format("Unknown value por EMSX_STATUS:{0}", EMSX_STATUS));
        }

        public string GetRejReason()
        {
            //En base a lo que hay en Message calcular el RejReason
            return "";
        }

        private decimal GetCumQty()
        {
            if (GetOrdStatus() == OrdStatus.Filled)
            {
                return Order.OrderQty;
            }
            else
            {
                decimal EMSX_FILLED = Convert.ToDecimal(Message.AsElement.GetElementAsFloat32("EMSX_FILLED"));
                return EMSX_FILLED;
            }
        }

        private decimal GetAvgPx()
        {
            decimal EMSX_AVG_PRICE = Convert.ToDecimal(Message.AsElement.GetElementAsFloat64("EMSX_AVG_PRICE"));

            return EMSX_AVG_PRICE;
        }

        private decimal GetLastQty()
        {
            decimal EMSX_LAST_SHARES = Convert.ToDecimal(Message.AsElement.GetElementAsFloat32("EMSX_LAST_SHARES"));

            return EMSX_LAST_SHARES;
        }

        private decimal GetLastPx()
        {
            decimal EMSX_LAST_PRICE = Convert.ToDecimal(Message.AsElement.GetElementAsFloat64("EMSX_LAST_PRICE"));

            return EMSX_LAST_PRICE;
        }

        private decimal GetLeavesQty()
        {
            return Order.OrderQty - GetCumQty();
        }


        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format("Execution Report for symbol {2}: Order Status={0} - Exec Type={1} {3}",
                GetOrdStatus(), GetExecType(), Order.Ticker, GetRejReason() != null ? string.Format("Rej. Reason:{0}", GetRejReason()) : "");
        }

        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.ExecType)
                return GetExecType();
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return GetOrdStatus();
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return GetLeavesQty();//Poner el valor correcto cuando tengamos alguna punta del verdadero Execution Report
            else if (xrField == ExecutionReportFields.CumQty)
                return GetCumQty();
            else if (xrField == ExecutionReportFields.AvgPx)
                return GetAvgPx();
            else if (xrField == ExecutionReportFields.Commission)
                return 0;
            else if (xrField == ExecutionReportFields.Text)
                return GetRejReason();
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return GetLastQty();
            else if (xrField == ExecutionReportFields.LastPx)
                return GetLastPx();
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

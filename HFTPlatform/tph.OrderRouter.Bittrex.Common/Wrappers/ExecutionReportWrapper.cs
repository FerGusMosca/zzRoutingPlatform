using Bittrex.Net.Enums;
using Bittrex.Net.Objects.Models.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;

namespace tph.OrderRouter.Bittrex.Common.Wrappers
{
    public class ExecutionReportWrapper : Wrapper
    {
        #region Private Static Consts


        #endregion

        #region Constructors

        public ExecutionReportWrapper(Order pOrder, BittrexOrderUpdate pBittrexOrderUpdate,bool isReplacement) {
            Order = pOrder;
            BittrexOrderUpdate = pBittrexOrderUpdate;
            IsReplacement = isReplacement;
        }

        #endregion

        #region Protected Atributes

        protected Order Order { get; set; } 

        protected BittrexOrderUpdate BittrexOrderUpdate { get; set; }
        

        protected bool IsReplacement { get; set; }

        #endregion

        #region Private Methods

        public ExecType GetExecTypeFromBittrexStatus()
        {
            if (BittrexOrderUpdate.Delta.Status == OrderStatus.Open)
            {
                if (Order.CumQty>0)
                    return ExecType.Trade;
                else
                    return ExecType.New;

            }
            else if (BittrexOrderUpdate.Delta.Status == OrderStatus.Closed)
            {
                if (BittrexOrderUpdate.Delta.Id == null)
                    return ExecType.Rejected;
                else
                {
                    if (BittrexOrderUpdate.Delta.QuantityFilled > 0)
                        return ExecType.Trade;
                    else
                    {
                        if (IsReplacement)
                        {
                            return ExecType.Replaced;

                        }
                        else
                        {

                            if (BittrexOrderUpdate.Delta.QuantityFilled > 0)
                                return ExecType.Trade;
                            else
                            {
                                return ExecType.Canceled;
                            }
                        }
                    }
                }
            }
            else
                return ExecType.Unknown;
        
        }

        public OrdStatus GetOrdStatusFromBittrexStatus()
        {

            if (BittrexOrderUpdate.Delta.Status == OrderStatus.Open)
            {
                if (Order.CumQty>0)
                    return OrdStatus.PartiallyFilled;
                else
                    return OrdStatus.New;

            }
            else if (BittrexOrderUpdate.Delta.Status == OrderStatus.Closed)
            {
                if (BittrexOrderUpdate.Delta.Id == null)
                    return OrdStatus.Rejected;
                else
                {

                    if (IsReplacement)
                    {
                        return OrdStatus.Replaced;

                    }
                    else {

                        if (BittrexOrderUpdate.Delta.QuantityFilled > 0)
                        {
                            if(BittrexOrderUpdate.Delta.Quantity> BittrexOrderUpdate.Delta.QuantityFilled )
                                return OrdStatus.PartiallyFilled;
                            else
                                return OrdStatus.Filled;
                        }
                        else
                        {
                            return OrdStatus.Canceled;
                        }
                    }
                }
            }
            else
                return OrdStatus.Unkwnown;

        }

        #endregion

        #region Overriden Methos
        public override Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        public override object GetField(Fields field)
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
                return BittrexOrderUpdate.Delta.Quantity- BittrexOrderUpdate.Delta.QuantityFilled;
            else if (xrField == ExecutionReportFields.CumQty)
                return Order.CumQty;
            else if (xrField == ExecutionReportFields.AvgPx)
                return BittrexOrderUpdate.Delta.Price;
            else if (xrField == ExecutionReportFields.Commission)
                return BittrexOrderUpdate.Delta.Fee;
            else if (xrField == ExecutionReportFields.Text)
                return Order.RejReason;
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return BittrexOrderUpdate.Delta.QuantityFilled-Order.CumQty;
            else if (xrField == ExecutionReportFields.LastPx)
                return Order.Price;
            else if (xrField == ExecutionReportFields.LastMkt)
                return ExecutionReportFields.NULL;

            if (Order == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return BittrexOrderUpdate.Delta.Id;
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
                return QuantityType.CURRENCY;
            else if (xrField == ExecutionReportFields.PriceType)
                return PriceType.FixedAmount;

            else
                return ExecutionReportFields.NULL;
        }

        #endregion
    }
}

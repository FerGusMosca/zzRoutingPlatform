using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstrFullMarketConnectivity.IOL.Common;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.InvertirOnline.Common.DTO;
using zHFT.OrderRouters.InvertirOnline.Common.Responses;

namespace zHFT.OrderRouters.InvertirOnline.Common.Wrappers
{
    public class ExecutionReportWrapper : Wrapper
    {
        #region Private Static Consts


        #endregion

        #region Constructors

        public ExecutionReportWrapper(Order pOrder, ExecutionReportResp pExecutionReportResp)
        {
            Order = pOrder;
            ExecutionReportResp = pExecutionReportResp;
        }

        #endregion 

        #region Private Attributes

        private ExecType GetExecTypeFromIOLStatus()
        {
            if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._INICIADA.ToUpper())
                return ExecType.PendingNew;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._EN_PROCESO.ToUpper())
                return ExecType.New;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._PARCIALMENTE_TERMINADA.ToUpper())
                return ExecType.Trade;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._TERMINADA.ToUpper())
                return ExecType.Trade;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._CANCELADA.ToUpper())
                return ExecType.Canceled;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._PENDINTE_CANCELACION.ToUpper())
                return ExecType.PendingCancel;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._CANCELADA_VTO_VALIDEZ.ToUpper())
                return ExecType.Expired;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._PARCIALMENTE_TERMINADA_CANCEL.ToUpper())
                return ExecType.PendingCancel;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._EN_MODIFICACION.ToUpper())
                return ExecType.PendingReplace;
            else
                return ExecType.Unknown;
        
        
        
        }

        protected OrdStatus GetOrdStatusFromIOLStatus()
        {
            if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._INICIADA.ToUpper())
                return OrdStatus.PendingNew;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._EN_PROCESO.ToUpper())
                return OrdStatus.New;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._PARCIALMENTE_TERMINADA.ToUpper())
                return OrdStatus.PartiallyFilled;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._TERMINADA.ToUpper())
                return OrdStatus.Filled;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._CANCELADA.ToUpper())
                return OrdStatus.Canceled;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._PENDINTE_CANCELACION.ToUpper())
                return OrdStatus.PendingCancel;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._CANCELADA_VTO_VALIDEZ.ToUpper())
                return OrdStatus.Expired;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._PARCIALMENTE_TERMINADA_CANCEL.ToUpper())
                return OrdStatus.PendingCancel;
            else if (ExecutionReportResp.estadoActual.ToUpper() == ExecutionReportResp._EN_MODIFICACION.ToUpper())
                return OrdStatus.PendingReplace;
            else
                return OrdStatus.Unkwnown;
        }

        protected ExecutionReportResp ExecutionReportResp { get; set; }

        protected Order Order { get; set; }

        #endregion

        #region Private Methods

        private double? GetLastQtyFromIOL()
        {
            if (ExecutionReportResp.operaciones != null && ExecutionReportResp.operaciones.Length>0)
            {
                
                Trade trade = ExecutionReportResp.operaciones.Where(x => x.fecha.HasValue).OrderByDescending(x => x.fecha.Value).FirstOrDefault();

                return trade.cantidad;

            }
            else
                return null;
        
        }

        private double? GetLastPxFromIOL()
        {

            if (ExecutionReportResp.operaciones != null && ExecutionReportResp.operaciones.Length > 0)
            {

                Trade trade = ExecutionReportResp.operaciones.Where(x => x.fecha.HasValue).OrderByDescending(x => x.fecha.Value).FirstOrDefault();

                return trade.precio;

            }
            else
                return null;
        }

        private string GetTextFromIOL()
        {

            if (ExecutionReportResp.estados != null && ExecutionReportResp.estados.Length > 0)
            {

                ExecutionReportState state = ExecutionReportResp.estados.Where(x => x.fecha.HasValue).OrderByDescending(x => x.fecha.Value).FirstOrDefault();

                return state.detalle;

            }
            else
                return null;
        }

        private DateTime GetTransactTimeFromIOL()
        {
            if (ExecutionReportResp.fechaOperado.HasValue)
                return ExecutionReportResp.fechaOperado.Value;
            else if (ExecutionReportResp.fechaAlta.HasValue)
                return ExecutionReportResp.fechaAlta.Value;
            else
                return DateTime.Now;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Order != null && ExecutionReportResp != null)
            {
                return string.Format("Symbol {0} ExecType {1} OrdStatus {2}", Order.simbolo, GetExecTypeFromIOLStatus(), GetOrdStatusFromIOLStatus());
            }
            else
                return "";
        }


        public override object GetField(Main.Common.Enums.Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (ExecutionReportResp == null || Order == null)
                return ExecutionReportFields.NULL;


            if (xrField == ExecutionReportFields.ExecType)
                return GetExecTypeFromIOLStatus();
            if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.OrdStatus)
                return GetOrdStatusFromIOLStatus();
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.LeavesQty)
                return Order.cantidad-ExecutionReportResp.cantidad;
            else if (xrField == ExecutionReportFields.CumQty)
                return ExecutionReportResp.cantidad;
            else if (xrField == ExecutionReportFields.AvgPx)
                return ExecutionReportResp.precio;
            else if (xrField == ExecutionReportFields.Commission)
                return ExecutionReportFields.NULL;
            else if (xrField == ExecutionReportFields.Text)
                return GetTextFromIOL();
            else if (xrField == ExecutionReportFields.TransactTime)
                return GetTransactTimeFromIOL();
            else if (xrField == ExecutionReportFields.LastQty)
                return GetLastQtyFromIOL();
            else if (xrField == ExecutionReportFields.LastPx)
                return GetLastPxFromIOL();
            else if (xrField == ExecutionReportFields.LastMkt)
                return ExecutionReportResp.mercado;

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

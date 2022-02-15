using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.InvertirOnline.Common.Responses
{
    public class ExecutionReportResp
    {

        #region Public Static Consts

        public static string _INICIADA = "iniciada";//1 - PendingNew
        public static string _EN_PROCESO = "en_proceso";//2 - New
        public static string _PARCIALMENTE_TERMINADA = "parcialmente_terminada";//3 - Partially Filled
        public static string _TERMINADA = "terminada";//4 - Filled
        public static string _CANCELADA = "cancelada";//5 - Cancelled
        public static string _PENDINTE_CANCELACION = "pendiente_cancelacion";//6 - PendingCancel
        public static string _CANCELADA_VTO_VALIDEZ = "cancelada_por_vencimiento_validez";//7 - Expired
        public static string _PARCIALMENTE_TERMINADA_CANCEL = "parcialmente_terminada_con_pedido_cancelacion";//8 - PendingCancel
        public static string _EN_MODIFICACION = "en_modificacion ";//9 - PendingReplace

        #endregion

        public bool IsOpenOrder()
        {
            return estadoActual.ToUpper() == _INICIADA.ToUpper() || estadoActual.ToUpper() == _EN_PROCESO.ToUpper()
                   || estadoActual.ToUpper() == _PARCIALMENTE_TERMINADA.ToUpper()
                   || estadoActual.ToUpper() == _PENDINTE_CANCELACION.ToUpper()
                   || estadoActual.ToUpper() == _PARCIALMENTE_TERMINADA_CANCEL.ToUpper()
                   || estadoActual.ToUpper() == _EN_MODIFICACION.ToUpper();
        }

        public bool IsCancelled()
        {
            //NOTa ESTO ES un bug de IOL
            //Cuando se cancela una orden, la marca como TERMINADA si es una orden Partially FIlled (cualquier cosa)
            return estadoActual.ToUpper() == _CANCELADA.ToUpper() || estadoActual.ToUpper() == _TERMINADA.ToUpper();
        
        }



        #region Public Attributes

        public int numero { get; set; }

        public string mercado { get; set; }

        public string simbolo { get; set; }

        public string moneda { get; set; }

        public string tipo { get; set; }


        public DateTime? fechaAlta { get; set; }

        public string validez { get; set; }

        public DateTime? fechaOperado { get; set; }

        public string estadoActual { get; set; }

        public ExecutionReportState[] estados { get; set; }

        //aranceles

        public Trade[] operaciones { get; set; }


        public decimal precio { get; set; }

        public double cantidad{ get; set; }

        public string monto { get; set; }

        public string modalidad { get; set; }

        #endregion

        #region Public Methods
        
        public OrdStatus GetOrdStatusFromIOLStatus()
        {
            if (estadoActual.ToUpper() == ExecutionReportResp._INICIADA.ToUpper())
                return OrdStatus.PendingNew;
            else if (estadoActual.ToUpper() == ExecutionReportResp._EN_PROCESO.ToUpper())
                return OrdStatus.New;
            else if (estadoActual.ToUpper() == ExecutionReportResp._PARCIALMENTE_TERMINADA.ToUpper())
                return OrdStatus.PartiallyFilled;
            else if (estadoActual.ToUpper() == ExecutionReportResp._TERMINADA.ToUpper())
                return OrdStatus.Filled;
            else if (estadoActual.ToUpper() == ExecutionReportResp._CANCELADA.ToUpper())
                return OrdStatus.Canceled;
            else if (estadoActual.ToUpper() == ExecutionReportResp._PENDINTE_CANCELACION.ToUpper())
                return OrdStatus.PendingCancel;
            else if (estadoActual.ToUpper() == ExecutionReportResp._CANCELADA_VTO_VALIDEZ.ToUpper())
                return OrdStatus.Expired;
            else if (estadoActual.ToUpper() == ExecutionReportResp._PARCIALMENTE_TERMINADA_CANCEL.ToUpper())
                return OrdStatus.PendingCancel;
            else if (estadoActual.ToUpper() == ExecutionReportResp._EN_MODIFICACION.ToUpper())
                return OrdStatus.PendingReplace;
            else
                return OrdStatus.Unkwnown;
        }

        public double GetCumQty()
        {
            //Esto es por otro bug de IOL
            if (GetOrdStatusFromIOLStatus() == OrdStatus.Filled)
                return cantidad;
            else
            {
                double cumQty = 0;

                if (operaciones != null)
                {
                    operaciones.ToList().ForEach(x => cumQty += x.cantidad);
                }

                return cumQty;
            }
        }


        #endregion


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return estadoActual.ToUpper() == _CANCELADA.ToUpper();
        
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

        public double GetCumQty()
        {
            double cumQty = 0;

            if (operaciones != null)
            {
                operaciones.ToList().ForEach(x => cumQty += x.cantidad);
            }
            return cumQty;
        }


        #endregion


    }
}

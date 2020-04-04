using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.InvertirOnline.Common.DTO
{
    public class BaseOrder
    {
        #region Invertir Online Attributes

        public string mercado { get; set; }

        public string simbolo { get; set; }

        public long cantidad { get; set; }

        public double? precio { get; set; }

        public string modalidad { get; set; }

        public string plazo { get; set; }

        public DateTime validez { get; set; }

        #endregion

        #region Public Static Methods

        public static BaseOrder Clone(BaseOrder oOrder)
        {
            BaseOrder bOrder = new BaseOrder();

            bOrder.mercado = oOrder.mercado;
            bOrder.simbolo = oOrder.simbolo;
            bOrder.cantidad = oOrder.cantidad;
            bOrder.precio = oOrder.precio;
            bOrder.plazo = oOrder.plazo;
            bOrder.validez = oOrder.validez;

            return bOrder;
        }

        #endregion
    }
}

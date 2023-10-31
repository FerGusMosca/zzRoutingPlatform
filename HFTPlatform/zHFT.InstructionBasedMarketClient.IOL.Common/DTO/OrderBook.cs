using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.IOL.Common.DTO
{
    public class OrderBook
    {
        #region Protected Methods

        public double cantidadCompra { get; set; }

        public double precioCompra { get; set; }

        public double precioVenta { get; set; }

        public double cantidadVenta { get; set; }

        #endregion
    }
}

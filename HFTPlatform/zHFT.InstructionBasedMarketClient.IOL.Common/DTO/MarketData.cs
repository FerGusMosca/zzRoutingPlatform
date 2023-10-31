using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.IOL.Common.DTO
{
    public class MarketData
    {
        #region Public Attributes

        public double ultimoPrecio { get; set; }

        public double variacion { get; set; }

        public double apertura { get; set; }

        public double maximo { get; set; }

        public double minimo { get; set; }

        public DateTime fechaHora { get; set; }

        public string tendencia { get; set; }

        public double cierreAnterior { get; set; }

        public double montoOperado { get; set; }

        public double volumenNominal { get; set; }

        public double precioPromedio { get; set; }

        public string moneda { get; set; }

        public double precioAjuste { get; set; }

        public double interesesAbiertos { get; set; }

        public int cantidadOperaciones { get; set; }

        public OrderBook[] puntas { get; set; }

        #endregion
    }
}

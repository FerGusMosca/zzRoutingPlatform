using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.IOL.Common.DTO
{
    public class Posicion
    {
        #region Public Attributes

        public decimal cantidad { get; set; }

        public decimal comprometido { get; set; }

        public decimal? puntosVariacion { get; set; }

        public decimal? variacionDiaria { get; set; }

        public decimal? ultimoPrecio { get; set; }

        public decimal ppc { get; set; }

        public decimal? gananciaPorcentaje { get; set; }

        public decimal? gananciaDinero { get; set; }

        public decimal? valorizado { get; set; }

        public Titulo titulo { get; set; }

        #endregion
    }
}

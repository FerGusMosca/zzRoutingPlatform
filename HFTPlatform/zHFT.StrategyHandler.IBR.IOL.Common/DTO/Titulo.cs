using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.IBR.IOL.Common.DTO
{
    public class Titulo
    {
        #region public Attributes

        public string simbolo { get; set; }

        public string descripcion { get; set; }

        public string pais { get; set; }

        public string mercado { get; set; }

        public string tipo { get; set; }

        public string plazo { get; set; }

        public bool operable { get; set; }

        public bool operableEnInmediato { get; set; }

        public bool tieneOpciones { get; set; }

        public string moneda { get; set; }

        #endregion
    }
}

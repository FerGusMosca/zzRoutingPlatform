using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.IOL.Common.DTO
{
    public class Fund
    {
        #region Public Attributes

        public decimal variacion { get; set; }

        public decimal ultimoOperado { get; set; }

        public string horizonteInversion { get; set; }

        public string rescate { get; set; }

        public string invierte { get; set; }

        public string tipofondo { get; set; }

        public string avisoHorarioEjecucion { get; set; }

        public string tipoAdministradoraTituloFCI { get; set; }

        public string fechaCorte { get; set; }

        public string codigoBloomberg { get; set; }

        public string perfilInversor { get; set; }

        public string informeMensual { get; set; }

        public string reglamentoGestion { get; set; }

        public string simbolo { get; set; }

        public string descripcion { get; set; }

        public string pais { get; set; }

        public string mercado { get; set; }

        public string plazo { get; set; }

        public string moneda { get; set; }

        public string tipo { get; set; }

        #endregion
    }
}

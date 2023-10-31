using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.OrderRouters.Bloomberg.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string IP { get; set; }

        public int Port { get; set; }

        public string Exchange { get; set; }

        public string Broker { get; set; }

        public string EMSX_Environment { get; set; }

        public int InitialOrderId { get; set; }

        #endregion

        #region Private Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }

            if (string.IsNullOrEmpty(IP))
            {
                result.Add("IP");
                resultado = false;
            }

            if (Port < 0)
            {
                result.Add("Port");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add("Exchange");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Broker))
            {
                result.Add("Broker");
                resultado = false;
            }

            if (string.IsNullOrEmpty(EMSX_Environment))
            {
                result.Add("EMSX_Environment");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

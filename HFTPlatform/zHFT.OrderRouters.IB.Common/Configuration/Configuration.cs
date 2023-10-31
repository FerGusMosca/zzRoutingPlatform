using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.OrderRouters.IB.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public bool Active { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public int IdIBClient { get; set; }

        public string Exchange { get; set; }

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

            if (IdIBClient < 0)
            {
                result.Add("IdIBClient");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add("Exchange");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

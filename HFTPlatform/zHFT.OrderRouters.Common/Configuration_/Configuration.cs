using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.OrderRouters.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string Proxy { get; set; }

        public string ProxyConfigFile { get; set; }

        public int OrderUpdateInMilliseconds  { get; set; }

        public int DOMUpdateInMilliseconds { get; set; }

        public int? OrderIdStart { get; set; }

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

            if (string.IsNullOrEmpty(Proxy))
            {
                result.Add("Proxy");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ProxyConfigFile))
            {
                result.Add("ProxyConfigFile");
                resultado = false;
            }

            if (OrderUpdateInMilliseconds<=0)
            {
                result.Add("OrderUpdateInMilliseconds");
                resultado = false;
            }

          
            return resultado;
        }

        #endregion
    }
}

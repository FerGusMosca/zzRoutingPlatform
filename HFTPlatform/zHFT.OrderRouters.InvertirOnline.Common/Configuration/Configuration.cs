using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.OrderRouters.InvertirOnline.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public int AccountNumber { get; set; }

        public string ConfigConnectionString { get; set; }

        public string MainURL { get; set; }

        public int CancellationTimeoutInSeconds { get; set; }

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

            if (AccountNumber < 0)
            {
                result.Add("AccountNumber");
                resultado = false;
            }

            if (string.IsNullOrEmpty(MainURL))
            {
                result.Add("MainURL");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ConfigConnectionString))
            {
                result.Add("ConfigConnectionString");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

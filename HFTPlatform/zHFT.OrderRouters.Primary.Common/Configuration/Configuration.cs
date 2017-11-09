using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.OrderRouters.Primary.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string FIXInitiatorPath { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        #endregion

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(FIXInitiatorPath))
            {
                result.Add("FIXInitiatorPath");
                resultado = false;
            }

            if (string.IsNullOrEmpty(User))
            {
                result.Add("User");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                result.Add("Password");
                resultado = false;
            }

            return resultado;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;

namespace zHFT.OrderRouters.Bitmex.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {        
        #region Public Attributes

        public string URL { get; set; }

        public string ApiKey { get; set; }
        public string Secret { get; set; }
 
        public int RefreshExecutionReportsInMilisec { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (!string.IsNullOrEmpty(URL))
            {
                result.Add("URL");
                resultado = false;
            }

            if (!string.IsNullOrEmpty(ApiKey))
            {
                result.Add("ApiKey");
                resultado = false;
            }

            if (!string.IsNullOrEmpty(Secret))
            {
                result.Add("Secret");
                resultado = false;
            }

            if (RefreshExecutionReportsInMilisec <= 0)
            {
                result.Add("RefreshExecutionReportsInMilisec");
                resultado = false;
            }

          
            return resultado;
        }

        #endregion
    }
}

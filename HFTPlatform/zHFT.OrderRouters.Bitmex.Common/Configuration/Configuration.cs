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

        public string RESTURL { get; set; }
        public string WebsocketURL { get; set; }

        public string ApiKey { get; set; }
        public string Secret { get; set; }
 
        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(RESTURL))
            {
                result.Add("RESTURL");
                resultado = false;
            }

            if (string.IsNullOrEmpty(WebsocketURL))
            {
                result.Add("WebsocketURL");
                resultado = false;
            }

            if (string.IsNullOrEmpty(ApiKey))
            {
                result.Add("ApiKey");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Secret))
            {
                result.Add("Secret");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

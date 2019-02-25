using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string RESTURL { get; set; }

        public string WebsocketURL { get; set; }
     
        #endregion

        #region Private Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(WebsocketURL))
            {
                result.Add("WebsocketURL");
                resultado = false;
            }

            if (string.IsNullOrEmpty(RESTURL))
            {
                result.Add("RESTURL");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

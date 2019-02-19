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
        public string URL { get; set; }
        public string ApiKey { get; set; }
        public string Secret { get; set; }
        public string QuoteCurrency { get; set; }
        public bool Simulate { get; set; }
        public int PublishUpdateInMilliseconds { get; set; }

     
        #endregion

        #region Private Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(URL))
            {
                result.Add("URL");
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

            if (string.IsNullOrEmpty(QuoteCurrency))
            {
                result.Add("QuoteCurrency");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.Binance.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string ApiKey { get; set; }
        public string Secret { get; set; }
        public string QuoteCurrency { get; set; }

        public string InstructionsAccessLayerConnectionString { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public int SearchForInstructionsInMilliseconds { get; set; }

        public int AccountNumber { get; set; }

        #endregion

        #region Private Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(QuoteCurrency))
            {
                result.Add("QuoteCurrency");
                resultado = false;
            }

            if (string.IsNullOrEmpty(InstructionsAccessLayerConnectionString))
            {
                result.Add("InstructionsAccessLayerConnectionString");
                resultado = false;
            }

            if (PublishUpdateInMilliseconds <= 0)
            {
                result.Add("PublishUpdateInMilliseconds");
                resultado = false;
            }


            if (SearchForInstructionsInMilliseconds <= 0)
            {
                result.Add("SearchForInstructionsInMilliseconds");
                resultado = false;
            }


            if (AccountNumber <= 0)
            {
                result.Add("AccountNumber");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

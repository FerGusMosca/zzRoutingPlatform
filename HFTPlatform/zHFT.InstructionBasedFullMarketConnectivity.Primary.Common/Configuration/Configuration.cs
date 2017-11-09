using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedFullMarketConnectivity.Primary.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string FIXInitiatorPath { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string InstructionsAccessLayerConnectionString { get; set; }

        public int SearchForInstructionsInMilliseconds { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public long AccountNumber { get; set; }

        public string Market { get; set; }

        public string MarketPrefixCode { get; set; }

        public string MarketClearingID { get; set; }

        #endregion

        #region Public Methods

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

            if (PublishUpdateInMilliseconds <= 0)
            {
                result.Add("PublishUpdateInMilliseconds");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Market))
            {
                result.Add("Market");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

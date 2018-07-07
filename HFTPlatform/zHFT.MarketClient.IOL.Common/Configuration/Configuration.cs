using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.MarketClient.IOL.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public int AccountNumber { get; set; }

        public int CredentialsAccountNumber { get; set; }

        public string InstructionsAccessLayerConnectionString { get; set; }

        public string ConfigConnectionString { get; set; }

        public string MainURL { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public int SearchForInstructionsInMilliseconds { get; set; }

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

            if (string.IsNullOrEmpty(ConfigConnectionString))
            {
                result.Add("ConfigConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(MainURL))
            {
                result.Add("MainURL");
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

            if (CredentialsAccountNumber <= 0)
            {
                result.Add("CredentialsAccountNumber");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

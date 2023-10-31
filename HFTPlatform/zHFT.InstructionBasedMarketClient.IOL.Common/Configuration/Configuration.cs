using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.IOL.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Static Consts

        public static string _SINGLE = "SINGLE";

        public static string _AUTPORTFOLIO = "AUTPORTFOLIO";

        #endregion


        #region Public Attributes

        public int AccountNumber { get; set; }

        public int CredentialsAccountNumber { get; set; }

        public string InstructionsAccessLayerConnectionString { get; set; }

        public string ConfigConnectionString { get; set; }

        public string MainURL { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public string Mode { get; set; }

        public string User { get; set; }

        public string Password { get; set; }


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


            if (PublishUpdateInMilliseconds <= 0)
            {
                result.Add("PublishUpdateInMilliseconds");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Mode))
            {
                result.Add("Mode");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

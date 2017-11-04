using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedMarketClient.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public bool Active { get; set; }

        public long AccountNumber { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public string InstructionsAccessLayerConnectionString { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public int SearchForInstructionsInMilliseconds { get; set; }

        public int IdIBClient { get; set; }

        public string Exchange { get; set; }

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

            if (string.IsNullOrEmpty(IP))
            {
                result.Add("IP");
                resultado = false;
            }

            if (AccountNumber<=0)
            {
                result.Add("AccountNumber");
                resultado = false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add(Exchange);
                resultado = false;
            }


            if (Port < 0)
            {
                result.Add("Port");
                resultado = false;
            }


            if (SearchForInstructionsInMilliseconds <= 0)
            {
                result.Add("SearchForInstructionsInMilliseconds");
                resultado = false;
            }

            if (PublishUpdateInMilliseconds <= 0)
            {
                result.Add("PublishUpdateInMilliseconds");
                resultado = false;
            }


            if (IdIBClient <= 0)
            {
                result.Add("IdIBClient");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.InstructionBasedFullMarketConnectivity.CoinApi.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string FIXInitiatorPath { get; set; }

        public string FIXMessageCreator { get; set; }

        public string InstructionsAccessLayerConnectionString { get; set; }

        public string SecuritiesAccessLayerConnectionString { get; set; }

        public string SecuritiesMarketTranslator { get; set; }

        public int SearchForInstructionsInMilliseconds { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public long AccountNumber { get; set; }

        public int MaxWaitingTimeForMarketDataRequest { get; set; }

        public bool RequestSecurityList { get; set; }

        public bool RequestFullMarketData { get; set; }

        public bool SaveFullMarketData { get; set; }


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

            if (string.IsNullOrEmpty(FIXMessageCreator))
            {
                result.Add("FIXMessageCreator");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SecuritiesAccessLayerConnectionString))
            {
                result.Add("SecuritiesAccessLayerConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(InstructionsAccessLayerConnectionString))
            {
                result.Add("InstructionsAccessLayerConnectionString");
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

            if (MaxWaitingTimeForMarketDataRequest <= 0)
            {
                result.Add("MaxWaitingTimeForMarketDataRequest");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

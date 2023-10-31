using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.StrategyHandler.MktDataDownloader.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public string MarketStartTime { get; set; }

        public string MarketEndTime { get; set; }

        public string ConnectionString { get; set; }

        public string SecurityType { get; set; }

        public string IncomingConfigPath { get; set; }

        public string IncomingModule { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {

            bool resultado = true;

            if (string.IsNullOrEmpty(MarketStartTime))
            {
                result.Add("MarketStartTime");
                resultado = false;
            }


            if (string.IsNullOrEmpty(MarketEndTime))
            {
                result.Add("MarketEndTime");
                resultado = false;
            }


            if (string.IsNullOrEmpty(ConnectionString))
            {
                result.Add("ConnectionString");
                resultado = false;
            }

            if (string.IsNullOrEmpty(SecurityType))
            {
                result.Add("SecurityType");
                resultado = false;
            }

            if (string.IsNullOrEmpty(IncomingConfigPath))
            {
                result.Add("IncomingConfigPath");
                resultado = false;
            }

            if (string.IsNullOrEmpty(IncomingModule))
            {
                result.Add("IncomingModule");
                resultado = false;
            }

            return resultado;
        }


        #endregion
    }
}

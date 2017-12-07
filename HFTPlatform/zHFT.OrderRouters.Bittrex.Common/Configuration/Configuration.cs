using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Abstract;

namespace zHFT.OrderRouters.Bittrex.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {
        #region Public Attributes

        public string ApiKey { get; set; }
        public string Secret { get; set; }
        public string QuoteCurrency { get; set; }
        public bool Simulate { get; set; }

        public int AccountNumber { get; set; }

        public string ConfigConnectionString { get; set; }

        public int RefreshExecutionReportsInMilisec { get; set; }

        #endregion

        #region Public Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;


            if (string.IsNullOrEmpty(QuoteCurrency))
            {
                result.Add("QuoteCurrency");
                resultado = false;
            }


            if (string.IsNullOrEmpty(ConfigConnectionString))
            {
                result.Add("ConfigConnectionString");
                resultado = false;
            }


            if (RefreshExecutionReportsInMilisec<=0)
            {
                result.Add("RefreshExecutionReportsInMilisec");
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

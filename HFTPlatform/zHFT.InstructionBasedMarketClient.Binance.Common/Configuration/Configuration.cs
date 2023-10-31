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
        public string QuoteCurrency { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public int? AccountNumber { get; set; }
        
        //public string EFConnectionString { get; set; }
        
        public string ConnectionString { get; set; }
        
        public string Key { get; set; }
        
        public string Secret { get; set; }

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

            if (PublishUpdateInMilliseconds <= 0)
            {
                result.Add("PublishUpdateInMilliseconds");
                resultado = false;
            }

            if (AccountNumber <= 0)
            {
                result.Add("AccountNumber");
                resultado = false;
            }
            
            //DBConnectionString
//            if (string.IsNullOrEmpty(EFConnectionString))
//            {
//                result.Add("EFConnectionString");
//                resultado = false;
//            }

            return resultado;
        }

        #endregion
    }
}

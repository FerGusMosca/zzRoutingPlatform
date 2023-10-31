using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.Bitmex.Common.DTO
{
    public class QueryResponse
    {
        #region Constructors

        public QueryResponse()
        {

            Headers = new Dictionary<string, string>();
        }

        #endregion

        #region Public Attributes

        public string Response { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        #endregion

        #region Public Methods

        public int GetRESTRateLimit()
        {
            string limit = Headers.Where(x => x.Key == "X-RateLimit-Remaining").FirstOrDefault().Value;

            return Convert.ToInt32(limit);

        }

        public string GetNextResetInSeconds()
        {

            int reset = Convert.ToInt32(Headers.Where(x => x.Key == "X-RateLimit-Reset").FirstOrDefault().Value);

            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime dateReset = epoch.AddSeconds(reset);


            return dateReset.ToString("dd/MM/yyyy hh:mm:ss");
        }


        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace zHFT.StrategyHandler.CLS.CoinMarketCap.DAL
{
    public class BaseAPIManager
    {

        #region Protected Attributes

        public string ApiKey { get; set; }

        #endregion

        #region Private Attributes

        private static object tAuthLock = new object();

        #endregion

        #region Constructors

        public BaseAPIManager(string pApiKey)
        {
            ApiKey = pApiKey;
        
        }

        #endregion

        #region Protected Methods

        protected string DoGetJson(string pURL, string convertParam)
        {

            var URL = new UriBuilder(pURL);

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["start"] = "1";
            queryString["limit"] = "1";
            queryString["convert"] = convertParam;

            URL.Query = queryString.ToString();

            var client = new WebClient();
            client.Headers.Add("X-CMC_PRO_API_KEY", ApiKey);
            client.Headers.Add("Accepts", "application/json");
            client.Headers.Add("Accept-Encoding", "deflate, gzip");
            return client.DownloadString(URL.ToString());
        }

        #endregion
    }
}

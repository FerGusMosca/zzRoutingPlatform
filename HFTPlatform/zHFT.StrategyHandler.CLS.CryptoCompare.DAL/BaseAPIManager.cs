using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.StrategyHandler.CLS.CryptoCompare.DAL
{
    public class BaseAPIManager
    {
        private object tAuthLock = new object();

        #region Protected Methods

        protected string DoGetJson(string url)
        {
            string content = string.Empty;
            lock (tAuthLock)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }
            }
            return content;
        }

        #endregion
    }
}

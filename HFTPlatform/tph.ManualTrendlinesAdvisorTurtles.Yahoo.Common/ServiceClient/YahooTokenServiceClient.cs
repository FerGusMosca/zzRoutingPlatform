using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace tph.ManualTrendlinesAdvisorTurtles.Yahoo.Common
{
    public class YahooTokenServiceClient
    {
        #region Public and Private Attributes

        public static string Cookie { get; set; }
        public static string Crumb { get; set; }
        private static Regex regex_crumb;

        private string URL { get; set; }

        #endregion

        #region Constructors

        public YahooTokenServiceClient(string pURL)
        {
            URL = pURL;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get crumb value from HTML
        /// </summary>
        /// <param name="html">HTML code</param>
        /// <returns></returns>
        private static string getCrumb(string html)
        {

            string crumb = null;

            try
            {
                //initialize on first time use
                if (regex_crumb == null)
                    regex_crumb = new Regex("CrumbStore\":{\"crumb\":\"(?<crumb>\\w+)\"}",
                        RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromSeconds(5));

                MatchCollection matches = regex_crumb.Matches(html);

                if (matches.Count > 0)
                {
                    crumb = matches[0].Groups["crumb"].Value;
                }
                else
                {
                    Debug.Print("Regex no match");
                }

                //prevent regex memory leak
                matches = null;

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            GC.Collect();
            return crumb;

        }

        #endregion

        #region Public Methods

        public bool Refresh(string symbol = "SPY")
        {

            if (Cookie != null && Crumb != null)
                return true;

            try
            {
                Cookie = "";
                Crumb = "";

                string url_scrape = URL;

                string url = string.Format(url_scrape, symbol);

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

                request.CookieContainer = new CookieContainer();
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {

                    string cookie = response.GetResponseHeader("Set-Cookie").Split(';')[0];

                    string html = "";

                    using (Stream stream = response.GetResponseStream())
                    {
                        html = new StreamReader(stream).ReadToEnd();
                    }

                    if (html.Length < 5000)
                        return false;
                    string crumb = getCrumb(html);
                    html = "";

                    if (crumb != null)
                    {
                        Cookie = cookie;
                        Crumb = crumb;
                        Debug.Print("Crumb: '{0}', Cookie: '{1}'", crumb, cookie);
                        return true;
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            return false;

        }


        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace tph.ManualTrendlinesAdvisorTurtles.Yahoo.Common
{
    public class YahooBase
    {
        #region Protected Consts

        protected const string _YAHOO_DECIMAL_SEPARATOR = ".";

        #endregion


        #region Protected Methods

        //credits to Dmitry Fedorkov
        //reference http://stackoverflow.com/questions/249760/how-to-convert-a-unix-timestamp-to-datetime-and-vice-versa
        protected double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            //Unix timestamp Is seconds past epoch
            return (dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        //credits to ScottCher
        //reference http://stackoverflow.com/questions/249760/how-to-convert-a-unix-timestamp-to-datetime-and-vice-versa
        protected DateTime UnixTimestampToDateTime(double unixTimeStamp)
        {
            //Unix timestamp Is seconds past epoch
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
        }

        protected string GetRaw(string events, string URL, string symbol, DateTime start, DateTime end, string Cookie, string Crumb)
        {

            string csvData = null;

            try
            {
                string url = URL + "period1={1}&period2={2}&interval=1d&events={3}&crumb={4}";

                // //if no token found, refresh it
                // if (string.IsNullOrEmpty(Cookie) | string.IsNullOrEmpty(Crumb))
                // {
                //     throw new Exception("A cookie and token must be specified");
                // }

                url = string.Format(url, symbol, Math.Round(DateTimeToUnixTimestamp(start), 0), Math.Round(DateTimeToUnixTimestamp(end), 0), events, Crumb);

                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.Cookie, Cookie);
                    csvData = wc.DownloadString(url);
                }

            }
            catch (WebException webEx)
            {
                HttpWebResponse response = (HttpWebResponse)webEx.Response;

                //Re-fecthing token
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new Exception("Unauthorized to access market data @Yahoo");
                else
                    throw;

            }
            catch (Exception ex)
            {
                throw;
            }

            return csvData;
        }


        #endregion
    }
}

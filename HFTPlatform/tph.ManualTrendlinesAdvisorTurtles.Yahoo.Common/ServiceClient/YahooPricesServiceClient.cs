using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ManualTrendlinesAdvisorTurtles.Yahoo.Common.DTO;

namespace tph.ManualTrendlinesAdvisorTurtles.Yahoo.Common
{
    public class YahooPricesServiceClient: YahooBase
    {
        #region Private Attributes

        private string URL { get; set; }

        private string Cookie { get; set; }

        private string Crumb { get; set; }

        #endregion

        #region Constructors

        public YahooPricesServiceClient(string pURL, string pCookie, string pCrumb)
        {
            URL = pURL;
            Cookie = pCookie;
            Crumb = pCrumb;
        }

        #endregion

        #region Private Methods

        private string GetRaw(string symbol, DateTime start, DateTime end)
        {
            return base.GetRaw("history", URL, symbol, start, end, Cookie, Crumb);
        }

        private List<Price> Map(string symbol, string csvData)
        {
            List<Price> prices = new List<Price>();
            string uiSep = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;

            string[] rows = csvData.Split(Convert.ToChar(10));

            //row(0) was ignored because is column names 
            //data is read from oldest to latest
            for (int i = 1; i <= rows.Length - 1; i++)
            {

                string row = rows[i];
                if (string.IsNullOrEmpty(row))
                    continue;

                string[] cols = row.Split(',');

                if (cols[1] == "null")
                    continue;

                Price price = new Price()
                {
                    Symbol = symbol,
                    Date = DateTime.Parse(cols[0]),
                    Open = decimal.Parse(cols[1].Replace(YahooBase._YAHOO_DECIMAL_SEPARATOR, uiSep), CultureInfo.CurrentUICulture),
                    Maximum = decimal.Parse(cols[2].Replace(YahooBase._YAHOO_DECIMAL_SEPARATOR, uiSep), CultureInfo.CurrentUICulture),
                    Minimum = decimal.Parse(cols[3].Replace(YahooBase._YAHOO_DECIMAL_SEPARATOR, uiSep), CultureInfo.CurrentUICulture),
                    Close = decimal.Parse(cols[4].Replace(YahooBase._YAHOO_DECIMAL_SEPARATOR, uiSep), CultureInfo.CurrentUICulture),
                    AdjClose = decimal.Parse(cols[5].Replace(YahooBase._YAHOO_DECIMAL_SEPARATOR, uiSep), CultureInfo.CurrentUICulture),
                    Volume = cols[6] != "null" ? Convert.ToInt64(decimal.Parse(cols[6].Replace(YahooBase._YAHOO_DECIMAL_SEPARATOR, uiSep), CultureInfo.CurrentUICulture)) : 0

                };

                prices.Add(price);
            }

            return prices;
        }

        #endregion

        #region Public Methods

        public List<Price> GetPrices(string symbol, DateTime startDate, DateTime endDate)
        {
            List<Price> prices = new List<Price>();

            string csvData = GetRaw(symbol, startDate, endDate);

            if (csvData != null)
            {
                prices = Map(symbol, csvData);
            }

            return prices;
        }


        #endregion
    }
}

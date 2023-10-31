using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.HistoricalPricesDownloader.Common.Configuration
{

    public class HistoricalPricesDownloaderConfiguration : BaseConfiguration
    {
        #region Public Attributes

        public string ConnectionString { get; set; }

        [XmlArray]
        [XmlArrayItem(ElementName = "Symbol")]
        public List<string> SymbolsToDownload { get; set; }

        public string SecurityType { get; set; }

        public string Currency { get; set; }

        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? From { get; set; }

        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? To { get; set; }//TODO Converters

        public int PacingBtwRequests { get; set; }

        public int PacingOnConnections { get; set; }

        
        public string Interval { get; set; }

        #endregion

        #region Private Static Const

        private static string _INTERVAL_MINUTE = "MINUTE";
        private static string _INTERVAL_DAY = "DAY";

        #endregion

        public CandleInterval GetCandleInterval()
        {

            if (Interval == _INTERVAL_MINUTE)
                return CandleInterval.Minute_1;
            else if (Interval == _INTERVAL_DAY)
                return CandleInterval.DAY;
            else
                throw new Exception($"{Name} Invalid Interaval value {Interval}");
        }

        public override bool CheckDefaults(List<string> result)
        {
            if (SymbolsToDownload == null || SymbolsToDownload.Count == 0)
            {
                result.Add("SymbolsToDownload");
                return false;
            }

            if (string.IsNullOrEmpty(ConnectionString))
            {
                result.Add("ConnectionString");
                return false;
            }

            if (string.IsNullOrEmpty(SecurityType))
            {
                result.Add("SecurityType");
                return false;
            }

            if (string.IsNullOrEmpty(Interval))
            {
                result.Add("Interval");
                return false;
            }

            return true;
        }
    }
}

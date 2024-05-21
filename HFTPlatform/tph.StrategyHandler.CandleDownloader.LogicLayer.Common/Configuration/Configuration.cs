using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;

namespace tph.StrategyHandler.CandleDownloader.Common.Configuration
{
    public class Configuration : BaseConfiguration
    {
        #region Protected Attributes


        public string ConnectionString { get; set; }

        public string SecurityType { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public string CandleReferencePrice { get; set; }



        [XmlArray]
        [XmlArrayItem(ElementName = "Symbol")]
        public List<string> SymbolsToDownload { get; set; }


        #endregion


        public override bool CheckDefaults(List<string> result)
        {

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

            if (string.IsNullOrEmpty(Currency))
            {
                result.Add("Currency");
                return false;
            }

            if (string.IsNullOrEmpty(Exchange))
            {
                result.Add("Exchange");
                return false;
            }

            if(SymbolsToDownload==null || SymbolsToDownload.Count==0)
            {
                result.Add("Exchange");
                return false;
            }


            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace zHFT.MarketClient.IB.Common.Configuration
{
    public class Configuration : BaseConfiguration, IConfiguration
    {
        #region Public Attributes

        public bool Active { get; set; }

        public string IP { get; set; }

        public int Port { get; set; }

        public string StockListAccessLayer { get; set; }

        public string StockListAccessLayerConnectionString { get; set; }

        public int PublishUpdateInMilliseconds { get; set; }

        public int IdPortfolio { get; set; }

        public int IdIBClient { get; set; }

        public bool? OnlyNotProcessed { get; set; }

        [XmlElement(ElementName = "Contract")]
        public List<Contract> ContractList { get; set; }

        public bool MarketDataDebugActive { get; set; }

        public int MarketDataDebugRefresh { get; set; }

        #endregion

        #region Private Methods

        public override bool CheckDefaults(List<string> result)
        {
            bool resultado = true;

            if (string.IsNullOrEmpty(Name))
            {
                result.Add("Name");
                resultado = false;
            }

            if (string.IsNullOrEmpty(IP))
            {
                result.Add("IP");
                resultado = false;
            }

            if (Port<0)
            {
                result.Add("Port");
                resultado = false;
            }


            if (PublishUpdateInMilliseconds<=0)
            {
                result.Add("PublishUpdateInMilliseconds");
                resultado = false;
            }


            if (IdIBClient <= 0)
            {
                result.Add("IdIBClient");
                resultado = false;
            }

            return resultado;
        }

        #endregion
    }
}

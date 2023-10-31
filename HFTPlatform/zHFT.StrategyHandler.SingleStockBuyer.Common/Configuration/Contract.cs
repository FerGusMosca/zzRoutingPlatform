using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace zHFT.StrategyHandler.SingleStockBuyer.Common.Configuration
{
    [Serializable()]
    [XmlRoot("Contract")]
    public class Contract
    {
        [XmlElement("Symbol")]
        public string Symbol { get; set; }

        [XmlElement("SecType")]
        public string SecType { get; set; }

        [XmlElement("Currency")]
        public string Currency { get; set; }

        [XmlElement("Exchange")]
        public string Exchange { get; set; }

        [XmlElement("Ammount")]
        public double Ammount { get; set; }

        [XmlElement("Side")]
        public string Side { get; set; }
    }
}

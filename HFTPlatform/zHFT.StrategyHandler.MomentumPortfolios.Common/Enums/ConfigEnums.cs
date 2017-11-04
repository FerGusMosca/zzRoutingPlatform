using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace zHFT.StrategyHandler.MomentumPortfolios.Common.Enums
{
    [Serializable]
    public enum Weight
    {
        [XmlEnum("E")]
        EW = 'E',
        [XmlEnum("V")]
        VW = 'V'
    }


    [Serializable]
    public enum FilterStocks
    {
        [XmlEnum("B")]
        B = 'B',//Big Cap
        [XmlEnum("M")]
        M = 'M',//Medium Cap
        [XmlEnum("S")]
        SM = 'S',//Small and Micro
        [XmlEnum("U")]
        ABSM = 'U',//All but Small and Micro
        [XmlEnum("A")]
        A = 'A',//All
    }

    [Serializable]
    public enum Ratio
    {
        [XmlEnum("C")]
        CAGR = 'C',
        [XmlEnum("H")]
        HQM = 'H' //High Quality Momentum
    }

    [Serializable]
    public enum Side
    {
        [XmlEnum("1")]
        BUY = '1',
        [XmlEnum("2")]
        SELL = '2' 
    }
}

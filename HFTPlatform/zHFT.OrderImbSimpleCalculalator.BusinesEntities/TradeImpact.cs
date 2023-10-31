using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;

namespace zHFT.OrderImbSimpleCalculator.BusinesEntities
{
    public enum ImpactSide 
    {
        Bid,
        Ask
    
    }

    public class TradeImpact
    {
        #region Public Attributes

        public MarketData MarketData { get; set; }

        public DateTime Timestamps { get; set; }

        public ImpactSide ImpactSide { get; set; }

        public double MDTradeSize { get; set; }

        public DateTime? LastTradeDateTime { get; set; }


        #endregion
    }
}

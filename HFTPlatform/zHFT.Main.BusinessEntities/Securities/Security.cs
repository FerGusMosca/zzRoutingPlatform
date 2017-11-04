using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Securities
{
    public class Security
    {

        #region Public Methods

        public string Symbol { get; set; }

        public string SecurityDesc { get; set; }

        public SecurityType SecType { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public MarketData MarketData { get; set; }

        public Halted? Halted { get; set; }

        #region Option Attributes

        public double? StrikePrice { get; set; }

        public DateTime? MaturityDate { get; set; }

        public string MaturityMonthYear { get; set; }

        public string SymbolSfx { get; set; }

        public string StrikeCurrency { get; set; }

        #endregion

        #region Contract Attributes

        public double? Factor { get; set; }

        public string CFICode { get; set; }

        public double? ContractMultiplier { get; set; }

        public double? MinPriceIncrement { get; set; }

        public double? TickSize { get; set; }

        public int? InstrumentPricePrecision { get; set; }

        public int? InstrumentSizePrecision { get; set; }

        public FinancingDetail FinancingDetails { get; set; }

        public SecurityTradingRule SecurityTradingRule { get; set; }

        public long? ContractPositionNumber { get; set; }

        #endregion

        #endregion

        #region Constructors

        public Security() 
        {
            MarketData = new MarketData();
        }

        #endregion

        #region Public Static Methods


        public static SecurityType GetSecurityType(string secType)
        {
            if(string.IsNullOrEmpty(secType))
                return SecurityType.OTH;

            if (secType.ToUpper() == SecurityType.CASH.ToString())
                return SecurityType.CASH;
            else if (secType.ToUpper() == SecurityType.CS.ToString())
                return SecurityType.CS;
            else if (secType.ToUpper() == SecurityType.FUT.ToString())
                return SecurityType.FUT;
            else if (secType.ToUpper() == SecurityType.IND.ToString())
                return SecurityType.IND;
            else if (secType.ToUpper() == SecurityType.OPT.ToString())
                return SecurityType.OPT;
            else if (secType.ToUpper() == SecurityType.OTH.ToString())
                return SecurityType.OTH;
            else
                return SecurityType.OTH;
        }

        #endregion
    }
}

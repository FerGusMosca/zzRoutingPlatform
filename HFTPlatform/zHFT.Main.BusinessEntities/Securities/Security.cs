using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Securities
{
    public class Security
    {
        #region Private Static Consts

        private static string _PUT_PREFIX = "P";
        private static string _CALL_PREFIX = "C";

        #endregion


        #region Public Methods

        public string Symbol { get; set; }

        public string AltIntSymbol { get; set; }

        public string SecurityDesc { get; set; }

        public SecurityType SecType { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public MarketData MarketData { get; set; }

        public Halted? Halted { get; set; }

        public bool Active { get; set; }

        #region Option Attributes

        public double? StrikePrice { get; set; }

        public DateTime? MaturityDate { get; set; }

        public string MaturityMonthYear { get; set; }

        public string SymbolSfx { get; set; }

        public string StrikeCurrency { get; set; }

        public PutOrCall? PutOrCall { get; set; }

        public int? StrikeMultiplier { get; set; }

        #endregion

        #region Contract Attributes

        public string UnderlyingSymbol { get; set; }

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

        public double? MarginRatio { get; set; }

        public decimal? ContractSize { get; set; }

        #endregion

        #region CryptoCurrency Attributes

        public bool ReverseMarketData { get; set; }

        #endregion

        #endregion

        #region Constructors

        public Security() 
        {
            MarketData = new MarketData();

            Active = true;
        }

        #endregion

        #region Public Methods

        public Security Clone(string newSymbol)
        {
            Security cloned = new Security();

            cloned.Symbol = newSymbol;
            cloned.SecType = SecType;
            cloned.Exchange = Exchange;

            return cloned;
        }

        public Security CallToPut()
        {

            Security cloned = new Security();
            cloned.SymbolSfx = SymbolSfx;
            cloned.StrikePrice = StrikePrice;
            cloned.MaturityDate = MaturityDate;
            cloned.MaturityMonthYear = MaturityMonthYear;
            cloned.Currency = Currency;
           
            if(PutOrCall.HasValue)
                cloned.PutOrCall = PutOrCall == zHFT.Main.Common.Enums.PutOrCall.Call ? zHFT.Main.Common.Enums.PutOrCall.Put : zHFT.Main.Common.Enums.PutOrCall.Call;
            
            cloned.StrikeMultiplier = StrikeMultiplier;
            cloned.Exchange = Exchange;
            cloned.AltIntSymbol = AltIntSymbol ;
            cloned.SecType = SecType;
            cloned.Symbol = cloned.BuildOptionSymbol();

            return cloned;

        }

        #endregion

        #region Public Static Methods


        public string GetFullSymbol()
        {
            if (Exchange != null)
            {
                if (AltIntSymbol != null)
                    return string.Format("{0}.{1}", AltIntSymbol, Exchange);
                else
                    return string.Format("{0}.{1}", Symbol, Exchange);
            }
            else
            {
                if (AltIntSymbol != null)
                    return string.Format("{0}", AltIntSymbol, Exchange);
                else
                    return string.Format("{0}", Symbol);
            }

        }

        public  string BuildOptionSymbol()
        {

            string rightCode = "?";
            if (PutOrCall.HasValue)
            {
                if (PutOrCall.Value == zHFT.Main.Common.Enums.PutOrCall.Call)
                {
                    rightCode = _CALL_PREFIX;
                }
                else if (PutOrCall.Value == zHFT.Main.Common.Enums.PutOrCall.Put)
                {
                    rightCode = _PUT_PREFIX;
                }
                else throw new Exception($"PutOrCall value not recognized:{PutOrCall.Value}");
            }

            string optionSymbol = $"{SymbolSfx}{rightCode}{MaturityMonthYear}{StrikePrice.Value.ToString("0.00")}";

            return optionSymbol;
        }

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
            else if (secType.ToUpper() == SecurityType.TB.ToString())
                return SecurityType.TB;
            else if (secType.ToUpper() == SecurityType.TBOND.ToString())
                return SecurityType.TBOND;
            else if (secType.ToUpper() == SecurityType.CMDTY.ToString())
                return SecurityType.CMDTY;
            else if (secType.ToUpper() == SecurityType.OTH.ToString())
                return SecurityType.OTH;
            else
                return SecurityType.OTH;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;

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
            cloned.UnderlyingSymbol= UnderlyingSymbol ;
            cloned.StrikeCurrency= StrikeCurrency;
            cloned.SecType = SecType;
            cloned.Symbol = cloned.BuildOptionSymbol();

            return cloned;

        }

        public override string ToString()
        {
            return $" Symbol={Symbol} Sec.Type={SecType} Currency={Currency}";
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
            return FullSymbolManager.GetSecurityTypeFromStr(secType);
        }

        public int RankSecurityType()
        {
            if (SecType == SecurityType.IND)//Always first the indexes
                return 1;
            else if (SecType == SecurityType.CASH)
                return 2;
            else if (SecType == SecurityType.CS)
                return 3;
            else if (SecType == SecurityType.FUT)
                return 4;
            else if (SecType == SecurityType.OPT)
                return 5;
            else if (SecType == SecurityType.TBOND)
                return 6;
            else if (SecType == SecurityType.TB)
                return 7;
            else if (SecType == SecurityType.IRS)
                return 8;
            else if (SecType == SecurityType.REPO)
                return 9;
            else if (SecType == SecurityType.CC)
                return 10;
            else if (SecType == SecurityType.CMDTY)
                return 11;
            else if (SecType == SecurityType.SWAP)
                return 12;
            else if (SecType == SecurityType.MF)
                return 13;
            else if (SecType == SecurityType.IND)
                return 14;
            else if (SecType == SecurityType.CASH)
                return 15;
            else if (SecType == SecurityType.OTH)
                return 16;
            else 
                return 17;


        }

        #endregion
    }
}

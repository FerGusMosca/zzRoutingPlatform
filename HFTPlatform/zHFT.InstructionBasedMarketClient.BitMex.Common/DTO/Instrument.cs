using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedMarketClient.BitMex.Common.DTO
{
    public class Instrument
    {
        #region Public Attributes

        public double? AskPrice { get; set; }
        public double? BankruptLimitDownPrice { get; set; }
        public double? BankruptLimitUpPrice { get; set; }
        public double? BidPrice { get; set; }
        public string BuyLeg { get; set; }
        public DateTime? CalcInterval { get; set; }
        public bool? Capped { get; set; }
        public DateTime? ClosingTimestamp { get; set; }
        public bool? Deleverage { get; set; }
        public DateTime? Expiry { get; set; }
        public double? FairBasis { get; set; }
        public double? FairBasisRate { get; set; }
        public string FairMethod { get; set; }
        public double? FairPrice { get; set; }
        public DateTime? Front { get; set; }
        public string FundingBaseSymbol { get; set; }
        public DateTime? FundingInterval { get; set; }
        public string FundingPremiumSymbol { get; set; }
        public string FundingQuoteSymbol { get; set; }
        public double? FundingRate { get; set; }
        public DateTime? FundingTimestamp { get; set; }
        public bool? HasLiquidity { get; set; }
        public double? HighPrice { get; set; }
        public double? ImpactAskPrice { get; set; }
        public double? ImpactBidPrice { get; set; }
        public double? ImpactMidPrice { get; set; }
        public double? IndicativeFundingRate { get; set; }
        public double? IndicativeSettlePrice { get; set; }
        public double? IndicativeTaxRate { get; set; }
        public double? InitMargin { get; set; }
        public double? InsuranceFee { get; set; }
        public string InverseLeg { get; set; }
        public bool? IsInverse { get; set; }
        public bool? IsQuanto { get; set; }
        public double? LastChangePcnt { get; set; }
        public double? LastPrice { get; set; }
        public double? LastPriceProtected { get; set; }
        public string LastTickDirection { get; set; }
        public double? Limit { get; set; }
        public double? LimitDownPrice { get; set; }
        public double? LimitUpPrice { get; set; }
        public DateTime? Listing { get; set; }
        public decimal? LotSize { get; set; }
        public double? LowPrice { get; set; }
        public double? MaintMargin { get; set; }
        public double? MakerFee { get; set; }
        public string MarkMethod { get; set; }
        public double? MarkPrice { get; set; }
        public decimal? MaxOrderQty { get; set; }
        public double? MaxPrice { get; set; }
        public double? MidPrice { get; set; }
        public decimal? Multiplier { get; set; }
        public DateTime? OpeningTimestamp { get; set; }
        public decimal? OpenInterest { get; set; }
        public decimal? OpenValue { get; set; }
        public double? OptionMultiplier { get; set; }
        public double? OptionStrikePcnt { get; set; }
        public double? OptionStrikePrice { get; set; }
        public double? OptionStrikeRound { get; set; }
        public double? OptionUnderlyingPrice { get; set; }
        public string PositionCurrency { get; set; }
        public double? PrevClosePrice { get; set; }
        public double? PrevPrice24h { get; set; }
        public decimal? PrevTotalTurnover { get; set; }
        public decimal? PrevTotalVolume { get; set; }
        public DateTime? PublishInterval { get; set; }
        public DateTime? PublishTime { get; set; }
        public string QuoteCurrency { get; set; }
        public decimal? QuoteToSettleMultiplier { get; set; }
        public DateTime? RebalanceInterval { get; set; }
        public DateTime? RebalanceTimestamp { get; set; }
        public string Reference { get; set; }
        public string ReferenceSymbol { get; set; }
        public DateTime? RelistInterval { get; set; }
        public decimal? RiskLimit { get; set; }
        public decimal? RiskStep { get; set; }
        public string RootSymbol { get; set; }
        public string SellLeg { get; set; }
        public DateTime? SessionInterval { get; set; }
        public string SettlCurrency { get; set; }
        public DateTime? Settle { get; set; }
        public double? SettledPrice { get; set; }
        public double? SettlementFee { get; set; }
        public string State { get; set; }
        public string Symbol { get; set; }
        public double? TakerFee { get; set; }
        public bool? Taxed { get; set; }
        public double? TickSize { get; set; }
        public DateTime? Timestamp { get; set; }
        public decimal? TotalTurnover { get; set; }
        public decimal? TotalVolume { get; set; }
        public decimal? Turnover { get; set; }
        public decimal? Turnover24h { get; set; }
        public string Typ { get; set; }
        public string Underlying { get; set; }
        public string UnderlyingSymbol { get; set; }
        public decimal? UnderlyingToPositionMultiplier { get; set; }
        public decimal? UnderlyingToSettleMultiplier { get; set; }
        public decimal? Volume { get; set; }
        public decimal? Volume24h { get; set; }
        public double? Vwap { get; set; }

        #endregion
    }
}

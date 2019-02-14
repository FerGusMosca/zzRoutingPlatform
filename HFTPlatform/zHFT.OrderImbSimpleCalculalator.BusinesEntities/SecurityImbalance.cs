using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class SecurityImbalance
    {
        #region Public Attributes

        public Security Security { get; set; }

        public DateTime DateTime { get; set; }

        public int CountTradeOnBid { get; set; }

        public decimal SizeTradeOnBid { get; set; }

        public int CountTradeOnAsk { get; set; }

        public decimal SizeTradeOnAsk { get; set; }

        public decimal BidCountImbalance
        {
            get {

                if ((CountTradeOnAsk + CountTradeOnBid) > 0)
                    return CountTradeOnBid / (CountTradeOnAsk + CountTradeOnBid);
                else
                    return 0;
            
            }
        
        
        }

        public decimal BidSizeImbalance
        {
            get
            {
                if (SizeTradeOnBid > 0 || SizeTradeOnAsk > 0)
                    return SizeTradeOnBid / (SizeTradeOnBid + SizeTradeOnAsk);
                else
                    return 0;

            }


        }

        public decimal AskCountImbalance
        {
            get
            {
                if ((CountTradeOnAsk + CountTradeOnBid) > 0)
                    return CountTradeOnAsk / (CountTradeOnAsk + CountTradeOnBid);
                else
                    return 0;
            }
        }

        public decimal AskSizeImbalance
        {
            get
            {
                if ((SizeTradeOnBid + SizeTradeOnAsk) > 0)
                    return SizeTradeOnAsk / (SizeTradeOnBid + SizeTradeOnAsk);
                else
                    return 0;
            }
        }

        #endregion

        #region Config Data

        public int DecimalRounding { get; set; }

        #endregion

        #region Statistical Data

        public DateTime? LastTradeProcessed { get; set; }

        public double? LastBidPrice { get; set; }

        public double? LastBidSize { get; set; }

        public double? LastAskPrice { get; set; }

        public double? LastAskSize { get; set; }

        public string ImbalanceSummary {

            get {

                return string.Format("{0} - Imbalance Bid:{1} Imbalance Ask {2}", Security.Symbol, BidSizeImbalance.ToString("0.##"), AskSizeImbalance.ToString("0.##"));
            }
        
        }

        #endregion

        #region Public Methods

        public void ProcessCounters()
        {

            if (Security.MarketData.LastTradeDateTime.HasValue && Security.MarketData.Trade.HasValue)
            {

                if (!LastTradeProcessed.HasValue ||
                    DateTime.Compare(LastTradeProcessed.Value, Security.MarketData.LastTradeDateTime.Value) != 0)
                {

                    if (Security.MarketData.Trade.HasValue && Security.MarketData.MDTradeSize.HasValue && LastBidPrice.HasValue
                        && Math.Round(Security.MarketData.Trade.Value, DecimalRounding) == Math.Round(LastBidPrice.Value, DecimalRounding))
                    {
                        CountTradeOnBid++;
                        SizeTradeOnBid += Convert.ToDecimal(Security.MarketData.MDTradeSize.Value);
                        LastTradeProcessed = Security.MarketData.LastTradeDateTime;

                    }

                    if (Security.MarketData.Trade.HasValue && Security.MarketData.MDTradeSize.HasValue && LastAskPrice.HasValue
                        && Math.Round(Security.MarketData.Trade.Value, DecimalRounding) == Math.Round(LastAskPrice.Value, DecimalRounding))
                    {
                        CountTradeOnAsk++;
                        SizeTradeOnAsk += Convert.ToDecimal(Security.MarketData.MDTradeSize.Value);
                        LastTradeProcessed = Security.MarketData.LastTradeDateTime;
                    }
                }
            }

            LastBidPrice = Security.MarketData.BestBidPrice;
            LastAskPrice = Security.MarketData.BestAskPrice;
            LastBidSize = Security.MarketData.BestBidSize;
            LastAskSize = Security.MarketData.BestAskSize;
        
        }

        public void ResetAll()
        {
            CountTradeOnAsk = 0;
            SizeTradeOnBid = 0;
            CountTradeOnAsk = 0;
            SizeTradeOnAsk = 0;
        }

        public bool LongPositionThresholdTriggered(decimal positionOpeningImbalanceThreshold)
        {
            return AskSizeImbalance > positionOpeningImbalanceThreshold;
        
        }

        public bool ShortPositionThresholdTriggered(decimal positionOpeningImbalanceThreshold)
        {
            return BidSizeImbalance > positionOpeningImbalanceThreshold;

        }

      

        #endregion
    }
}

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

                return CountTradeOnBid / (CountTradeOnAsk + CountTradeOnAsk);
            
            }
        
        
        }

        public decimal BidSizeImbalance
        {
            get
            {

                return SizeTradeOnBid / (SizeTradeOnBid + SizeTradeOnAsk);

            }


        }

        public decimal AskCountImbalance
        {
            get
            {
                return CountTradeOnAsk / (CountTradeOnAsk + CountTradeOnAsk);
            }
        }

        public decimal AskSizeImbalance
        {
            get
            {
                return SizeTradeOnAsk / (SizeTradeOnBid + SizeTradeOnAsk);
            }
        }

        #endregion

        #region Statistical Data

        public DateTime? LastTradeProcessed { get; set; }

        public double? LastBidPrice { get; set; }

        public double? LastBidSize { get; set; }

        public double? LastAskPrice { get; set; }

        public double? LastAskSize { get; set; }

        #endregion

        #region Public Methods

        public void ProcessCounters()
        {

            if (Security.MarketData.LastTradeDateTime.HasValue && Security.MarketData.Trade.HasValue)
            {

                if (!LastTradeProcessed.HasValue ||
                    DateTime.Compare(LastTradeProcessed.Value, Security.MarketData.LastTradeDateTime.Value) != 0)
                {

                    if (Security.MarketData.Trade.HasValue && Security.MarketData.MDTradeSize.HasValue && LastBidPrice.HasValue && Security.MarketData.Trade.Value == LastBidPrice.Value)
                    {
                        CountTradeOnBid++;
                        SizeTradeOnBid += Convert.ToDecimal(Security.MarketData.MDTradeSize.Value);

                    }

                    if (Security.MarketData.Trade.HasValue && Security.MarketData.MDTradeSize.HasValue && LastAskPrice.HasValue && Security.MarketData.Trade.Value == LastAskPrice.Value)
                    {
                        CountTradeOnAsk++;
                        SizeTradeOnAsk += Convert.ToDecimal(Security.MarketData.MDTradeSize.Value);

                    }

                }
            }

            LastTradeProcessed = Security.MarketData.LastTradeDateTime;
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

        #endregion
    }
}

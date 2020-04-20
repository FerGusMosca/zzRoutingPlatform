using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.OrderImbSimpleCalculator.BusinesEntities;

namespace zHFT.OrderImbSimpleCalculator.BusinessEntities
{
    public class ImbalanceCounter
    {
        #region Constructors

        public ImbalanceCounter()
        {
            TradeImpacts = new List<TradeImpact>();
            ActiveBlocks = new List<ImbalanceCounter>();
            StartTime = DateTime.Now;
        }

        #endregion

        #region Public Attributes

        public DateTime StartTime { get; set; }

        public DateTime? LastTradeProcessed { get; set; }

        public double? LastBidPrice { get; set; }

        public double? LastBidSize { get; set; }

        public double? LastAskPrice { get; set; }

        public double? LastAskSize { get; set; }

        public int CountTradeOnBid { get; set; }

        public decimal SizeTradeOnBid { get; set; }

        public int CountTradeOnAsk { get; set; }

        public decimal SizeTradeOnAsk { get; set; }

        public decimal BidCountImbalance
        {
            get
            {

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

        public List<TradeImpact> TradeImpacts { get; set; }

        public List<ImbalanceCounter> ActiveBlocks { get; set; }

        #endregion

        #region Private Methods

        private void UpdateTradeImpact(Security Security, ImpactSide side)
        {
            TradeImpacts.Add(new TradeImpact()
            {
                ImpactSide = side,
                LastTradeDateTime = Security.MarketData.LastTradeDateTime,
                MarketData = Security.MarketData,
                MDTradeSize = Security.MarketData.MDTradeSize.Value,
                Timestamps = DateTime.Now
            });
        }

        #endregion

        #region Public Metods


        public void ProcessCounters(Security Security, int? DecimalRounding)
        {

            if (Security.MarketData.LastTradeDateTime.HasValue && Security.MarketData.Trade.HasValue)
            {

                if (!LastTradeProcessed.HasValue ||
                    DateTime.Compare(LastTradeProcessed.Value, Security.MarketData.LastTradeDateTime.Value) != 0)
                {

                    if (Security.MarketData.Trade.HasValue && Security.MarketData.MDTradeSize.HasValue && LastBidPrice.HasValue
                        && Math.Round(Security.MarketData.Trade.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2) == Math.Round(LastBidPrice.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2))
                    {
                        CountTradeOnBid++;
                        SizeTradeOnBid += Convert.ToDecimal(Security.MarketData.MDTradeSize.Value);
                        LastTradeProcessed = Security.MarketData.LastTradeDateTime;
                        UpdateTradeImpact(Security,ImpactSide.Bid);

                    }

                    if (Security.MarketData.Trade.HasValue && Security.MarketData.MDTradeSize.HasValue && LastAskPrice.HasValue
                        && Math.Round(Security.MarketData.Trade.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2) == Math.Round(LastAskPrice.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2))
                    {
                        CountTradeOnAsk++;
                        SizeTradeOnAsk += Convert.ToDecimal(Security.MarketData.MDTradeSize.Value);
                        LastTradeProcessed = Security.MarketData.LastTradeDateTime;
                        UpdateTradeImpact(Security, ImpactSide.Ask);
                    }
                }
            }

            LastBidPrice = Security.MarketData.BestBidPrice;
            LastAskPrice = Security.MarketData.BestAskPrice;
            LastBidSize = Security.MarketData.BestBidSize;
            LastAskSize = Security.MarketData.BestAskSize;

        }

        public void ResetOldBlocks()
        {
            ImbalanceCounter firstDeltaCounter = ActiveBlocks.OrderBy(x => x.StartTime).FirstOrDefault();

            if (firstDeltaCounter != null)
            {
                CountTradeOnAsk -= firstDeltaCounter.CountTradeOnAsk;
                SizeTradeOnAsk -= firstDeltaCounter.SizeTradeOnAsk;
                CountTradeOnBid -= firstDeltaCounter.CountTradeOnBid;
                SizeTradeOnBid -= firstDeltaCounter.SizeTradeOnBid;
            }


            ActiveBlocks.Remove(firstDeltaCounter);
        }

        public void PersistCounters()
        {

            ImbalanceCounter deltaCounter = new ImbalanceCounter()
            {
                CountTradeOnAsk = CountTradeOnAsk ,
                SizeTradeOnAsk = SizeTradeOnAsk,
                CountTradeOnBid = CountTradeOnBid,
                SizeTradeOnBid = SizeTradeOnBid,
                LastTradeProcessed = LastTradeProcessed,
                LastBidPrice = LastBidPrice,
                LastBidSize = LastBidSize,
                LastAskPrice = LastAskPrice,
                LastAskSize = LastAskSize
            };

            foreach (ImbalanceCounter block in ActiveBlocks.OrderByDescending(x => x.StartTime))
            {
                deltaCounter.CountTradeOnAsk -= block.CountTradeOnAsk;
                deltaCounter.SizeTradeOnAsk -= block.SizeTradeOnAsk;
                deltaCounter.CountTradeOnBid -= block.CountTradeOnBid;
                deltaCounter.SizeTradeOnBid -= block.SizeTradeOnBid;
            
            }

            ActiveBlocks.Add(deltaCounter);
        
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

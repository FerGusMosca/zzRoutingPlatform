using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;

namespace tph.ChainedTurtles.BusinessEntities
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

    public class MonChainedImbalanceTurtleIndicator : MonChainedTurtleIndicator
    {

        #region Protected Attriutes

        protected List<TradeImpact> TradeImpacts { get; set; }

        protected DateTime LastCounterResetTime { get; set; }

        protected DateTime StartTime { get; set; }


        protected int BlockSizeInMinutes { get; set; }

        protected int ActiveBlocksSetting { get; set; }


        protected List<MonChainedImbalanceTurtleIndicator> ActiveBlocks { get; set; }

        protected string MarketStartTime { get; set; }

        protected string MarketEndTime { get; set; }

        protected object tLock { get; set; }


        #endregion


        #region Public Attributs

        public Security Security { get; set; }
        public MarketData LastProcessed { get; set; }

        public int CountTradeOnBid { get; set; }

        public int CountTradeOnAsk { get; set; }

        public decimal SizeTradeOnBid { get; set; }

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

        protected Thread ResetEveryNMinutesThread { get; set; }

        #endregion

        #region Constructors

        //Only as DTO constructor
        public MonChainedImbalanceTurtleIndicator() : base(null, null, null, null, null, false)
        { 
        
        
        }

        public MonChainedImbalanceTurtleIndicator(Security pSecurity, TurtlesCustomConfig pTurtlesCustomConfig, string candleRefPrice, string pCode, string signalType, bool reqPrices) : base(pSecurity, pTurtlesCustomConfig, candleRefPrice, pCode, signalType, reqPrices)
        {
            Security = pSecurity;

            LastProcessed = null;
            CountTradeOnBid = 0;
            CountTradeOnAsk = 0;    
            SizeTradeOnBid = 0;
            SizeTradeOnAsk = 0;

            LastCounterResetTime = DateTime.Now;
            StartTime = DateTime.Now;

            
            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);

            tLock = new object();
            ActiveBlocks = new List<MonChainedImbalanceTurtleIndicator>();
            TradeImpacts =new List<TradeImpact>();

            ResetEveryNMinutesThread = new Thread(ResetEveryNMinutes);
            ResetEveryNMinutesThread.Start();

        }


        #endregion

        #region Private Methods

        private void EvalTime(string time)
        {

            try
            {
                DateTime extrTime =MarketTimer.GetTodayDateTime(time);
                //If we got here, it worked ok
            }
            catch (Exception ex)
            {
                throw new Exception($"The following time is not properly formatted: {time}");
            }

        }

        private void LoadConfigValues(string customConfig)
        {
            //
            try
            {
                ImbalanceTurtleIndicatorConfigDTO resp= JsonConvert.DeserializeObject<ImbalanceTurtleIndicatorConfigDTO>(customConfig);


                if (!string.IsNullOrEmpty(resp.marketStartTime))
                {
                    EvalTime(resp.marketStartTime);
                    MarketStartTime = resp.marketStartTime;
                }
                else
                    throw new Exception("Missing config value marketStartTime");

                if (!string.IsNullOrEmpty(resp.marketEndTime))
                {
                    EvalTime(resp.marketEndTime);
                    MarketEndTime = resp.marketEndTime;
                }
                else
                    throw new Exception("Missing config value marketEndTime");


                if (resp.blockSizeInMinutes>0)
                    BlockSizeInMinutes = resp.blockSizeInMinutes;
                else
                    throw new Exception("config value blockSizeInMinutes must be greater than 0");


                if (resp.activeBlocksSetting > 0)
                    ActiveBlocksSetting = resp.activeBlocksSetting;
                else
                    throw new Exception("config value activeBlocksSetting must be greater than 0");



            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error deserializing custom config for symbol {Security.Symbol}:{ex.Message} ");
            }
        }

        public void ResetCounters()
        {
            CountTradeOnAsk = 0;
            SizeTradeOnAsk = 0;
            CountTradeOnBid = 0;
            SizeTradeOnBid = 0;

            LastProcessed = null;
       
            ActiveBlocks.Clear();
        }

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


        #region Protected Methods


        protected void ProcessCounters(MarketData marketdata, int? DecimalRounding)
        {

            if (LastProcessed != null && LastProcessed.LastTradeDateTime.HasValue && LastProcessed.Trade.HasValue)
            {

                if (!LastProcessed.GetReferenceDateTime().HasValue ||
                    DateTime.Compare(LastProcessed.GetReferenceDateTime().Value, marketdata.LastTradeDateTime.Value) != 0)
                {

                    if (marketdata.Trade.HasValue && marketdata.MDTradeSize.HasValue && LastProcessed.BestBidPrice.HasValue
                        && Math.Round(marketdata.Trade.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2) <= Math.Round(LastProcessed.BestBidPrice.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2))
                    {
                        CountTradeOnBid++;
                        SizeTradeOnBid += Convert.ToDecimal(marketdata.MDTradeSize.Value);
                        LastProcessed = marketdata;
                        UpdateTradeImpact(Security, ImpactSide.Bid);

                    }

                    if (Security.MarketData.Trade.HasValue && Security.MarketData.MDTradeSize.HasValue && LastProcessed.BestAskPrice.HasValue
                        && Math.Round(marketdata.Trade.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2) >= Math.Round(LastProcessed.BestAskPrice.Value, DecimalRounding.HasValue ? DecimalRounding.Value : 2))
                    {
                        CountTradeOnAsk++;
                        SizeTradeOnAsk += Convert.ToDecimal(Security.MarketData.MDTradeSize.Value);
                        LastProcessed = marketdata;
                        UpdateTradeImpact(Security, ImpactSide.Ask);
                    }
                }
            }
            else
            {
                LastProcessed = marketdata;
            }
        }

        private void ResetEveryNMinutes(object param)
        {
            if (BlockSizeInMinutes == 0 || ActiveBlocksSetting == 0)
                return;

            while (true)
            {
                TimeSpan elapsed = DateTime.Now - LastCounterResetTime;

                if(!zHFT.Main.Common.Util.MarketTimer.ValidMarketTime(MarketStartTime, MarketEndTime))
                {
                    ResetCounters();
                    LastCounterResetTime = DateTime.Now;
                    StartTime = DateTime.Now;

                }

                //Every BlockSize y save what I had in memory with the counters for every position
                if (elapsed.TotalMinutes > BlockSizeInMinutes)
                {
                    lock (tLock)
                    {


                        PersistCounters();

                        if (ActiveBlocks.Count > ActiveBlocksSetting)
                            ResetOldBlocks();

                       
                        LastCounterResetTime = DateTime.Now;
                    }
                }

                Thread.Sleep(1000);
            }
        }


        private void ResetOldBlocks()
        {
            var firstDeltaCounter = ActiveBlocks.OrderBy(x => x.StartTime).FirstOrDefault();

            if (firstDeltaCounter != null)
            {
                CountTradeOnAsk -= firstDeltaCounter.CountTradeOnAsk;
                SizeTradeOnAsk -= firstDeltaCounter.SizeTradeOnAsk;
                CountTradeOnBid -= firstDeltaCounter.CountTradeOnBid;
                SizeTradeOnBid -= firstDeltaCounter.SizeTradeOnBid;
            }


            ActiveBlocks.Remove(firstDeltaCounter);
        }

        private void PersistCounters()
        {

            MonChainedImbalanceTurtleIndicator deltaCounter = new MonChainedImbalanceTurtleIndicator()
            {
                CountTradeOnAsk = CountTradeOnAsk,
                SizeTradeOnAsk = SizeTradeOnAsk,
                CountTradeOnBid = CountTradeOnBid,
                SizeTradeOnBid = SizeTradeOnBid,
                LastProcessed=LastProcessed
            };

            foreach (var block in ActiveBlocks.OrderByDescending(x => x.StartTime))
            {
                deltaCounter.CountTradeOnAsk -= block.CountTradeOnAsk;
                deltaCounter.SizeTradeOnAsk -= block.SizeTradeOnAsk;
                deltaCounter.CountTradeOnBid -= block.CountTradeOnBid;
                deltaCounter.SizeTradeOnBid -= block.SizeTradeOnBid;

            }

            ActiveBlocks.Add(deltaCounter);

        }


        #endregion


        #region Base Overriden Methods


        public override bool EvalSignalTriggered()
        {
            bool longSignal = LongSignalTriggered();
            bool shortSignal = ShortSignalTriggered();

            return longSignal || shortSignal;

        }

        public override bool AppendCandle(MarketData md)
        {
            bool newCandle= base.AppendCandle(md);
            ProcessCounters(md,null);

            return newCandle;
        }


        public override bool LongSignalTriggered()
        {
            //TODO--> Long imbalance activated?
            return false;
        
        }


        public override bool ShortSignalTriggered()
        {
            //TODO --> Short imbalance activated?
            return false;

        }


        //EvalClosingShortPosition --> Uses standard closing mechanism
        //EvalClosingLongPosition -->  Uses standard closing mehcanism


        #endregion
    }
}

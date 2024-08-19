using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Util;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
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

        protected decimal PositionOpeningImbalanceThreshold { get; set; }

        protected decimal PositionClosingImbalanceThreshold { get; set; }


        protected List<MonChainedImbalanceTurtleIndicator> ActiveBlocks { get; set; }

        protected string MarketStartTime { get; set; }

        protected string MarketEndTime { get; set; }

        protected object tLock { get; set; }

        public string ImbSignalTriggered { get; set; }

        protected ILogger Logger { get; set; }


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
        public MonChainedImbalanceTurtleIndicator() : base(null, null, null)
        { 
        
        
        }

        public MonChainedImbalanceTurtleIndicator(Security pSecurity, TurtlesCustomConfig pTurtlesCustomConfig, string pCode, ILogger pLogger) : base(pSecurity, pTurtlesCustomConfig, pCode)
        {
            Security = pSecurity;

            LastProcessed = null;
            CountTradeOnBid = 0;
            CountTradeOnAsk = 0;    
            SizeTradeOnBid = 0;
            SizeTradeOnAsk = 0;

            Logger = pLogger;

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



        private void LoadConfigValues(string customConfig)
        {
            //
            try
            {
                ImbalanceTurtleIndicatorConfigDTO resp= JsonConvert.DeserializeObject<ImbalanceTurtleIndicatorConfigDTO>(customConfig);


                MarketStartTime = TurtleIndicatorBaseConfigLoader.GetMarketStartTime(resp);
                MarketEndTime = TurtleIndicatorBaseConfigLoader.GetMarketEndTime(resp);
                ClosingTime = TurtleIndicatorBaseConfigLoader.GetClosingTime(resp);
                RequestHistoricalPrices = TurtleIndicatorBaseConfigLoader.GetRequestHistoricalPrices(resp, true);

                if (RequestHistoricalPrices)
                    HistoricalPricesPeriod = TurtleIndicatorBaseConfigLoader.GetHistoricalPricesPeriod(resp);
                else
                    HistoricalPricesPeriod = 0;


                if (resp.blockSizeInMinutes>0)
                    BlockSizeInMinutes = resp.blockSizeInMinutes;
                else
                    throw new Exception("config value blockSizeInMinutes must be greater than 0");


                if (resp.activeBlocksSetting > 0)
                    ActiveBlocksSetting = resp.activeBlocksSetting;
                else
                    throw new Exception("config value activeBlocksSetting must be greater than 0");


                if (resp.positionOpeningImbalanceThreshold > 0)
                    PositionOpeningImbalanceThreshold = resp.positionOpeningImbalanceThreshold;
                else
                    throw new Exception("config value positionOpeningImbalanceThreshold must be greater than 0");


                if (resp.positionClosingImbalanceThreshold > 0)
                    PositionClosingImbalanceThreshold = resp.positionClosingImbalanceThreshold;
                else
                    throw new Exception("config value positionClosingImbalanceThreshold must be greater than 0");

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

                    Logger.DoLog($" Ind. Imbalance for {Security.Symbol}-->  CountTradeOnBid={CountTradeOnBid} SizeTradeOnBid={SizeTradeOnBid} " +
                                $"CountTradeOnAsk={CountTradeOnAsk} SizeTradeOnAsk={SizeTradeOnAsk}: " +
                                $"AskSizeImbalance={Math.Round(AskSizeImbalance,2)} BidSizeImbalance={Math.Round(BidSizeImbalance,2)}", Constants.MessageType.Information);
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
                    Logger.DoLog($"Reseting counters for symbol {Security.Symbol}", Constants.MessageType.Information);
                    lock (tLock)
                    {

                        Logger.DoLog($"Reseting counters for symbol {Security.Symbol}: CountTradeOnAsk:{CountTradeOnAsk} CountTradeOnBid:{CountTradeOnBid} SizeTradeOnAsk:{SizeTradeOnBid} SizeTradeOnAsk:{SizeTradeOnBid}--> AskSizeImbalance={BidSizeImbalance} AskSizeImbalance={BidSizeImbalance}", Constants.MessageType.Information);
                        PersistCounters();

                        if (ActiveBlocks.Count > ActiveBlocksSetting)
                            ResetOldBlocks();

                        Logger.DoLog($"NEW counters for symbol {Security.Symbol}: CountTradeOnAsk:{CountTradeOnAsk} CountTradeOnBid:{CountTradeOnBid} SizeTradeOnAsk:{SizeTradeOnBid} SizeTradeOnAsk:{SizeTradeOnBid}--> AskSizeImbalance={BidSizeImbalance} AskSizeImbalance={BidSizeImbalance}", Constants.MessageType.Information);
                        LastCounterResetTime = DateTime.Now;
                        Logger.DoLog($"Last counters reset time for symbol {Security.Symbol}:{LastCounterResetTime}", Constants.MessageType.Information);
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
            
            if (ActiveBlocks.Count >= ActiveBlocksSetting)
            {

                if (AskSizeImbalance >= PositionOpeningImbalanceThreshold)
                {
                    ImbSignalTriggered = $"LONG Imbalance for {Security.Symbol}: Ask Imbalance: {AskSizeImbalance}";

                    return true;
                }
                else
                {
                    Logger.DoLog($"{Security.Symbol}-->Ask Imbalance = {AskSizeImbalance} - Min. Trigger Imbalance = {PositionOpeningImbalanceThreshold}", Constants.MessageType.Information);
                    return false;
                }
            }
            
            else
            {
                Logger.DoLog($"{Security.Symbol}--> LONG Active Blocks = {ActiveBlocks.Count} - Min Blocks = {ActiveBlocksSetting}", Constants.MessageType.Information);
                return false;
            }
            
            
        
        }


        public override bool ShortSignalTriggered()
        {
            if (ActiveBlocks.Count >= ActiveBlocksSetting)
            {

                if (BidSizeImbalance >= PositionOpeningImbalanceThreshold)
                {
                    ImbSignalTriggered = $"SHORT Imbalance for {Security.Symbol}: Bid Imbalance: {BidSizeImbalance}";

                    return true;
                }
                else
                {
                    Logger.DoLog($"{Security.Symbol}-->Bid Imbalance = {AskSizeImbalance} - Min. Trigger Imbalance = {PositionOpeningImbalanceThreshold}", Constants.MessageType.Information);
                    return false;
                }
            }
            else
            {
                Logger.DoLog($"{Security.Symbol}--> SHORT Active Blocks = {ActiveBlocks.Count} - Min Blocks = {ActiveBlocksSetting}", Constants.MessageType.Information);
                return false;
            }

        }


        public override string SignalTriggered()
        {
            return ImbSignalTriggered;
        }


        //EvalClosingShortPosition --> Uses standard closing mechanism
        //EvalClosingLongPosition -->  Uses standard closing mehcanism

        #endregion
    }
}

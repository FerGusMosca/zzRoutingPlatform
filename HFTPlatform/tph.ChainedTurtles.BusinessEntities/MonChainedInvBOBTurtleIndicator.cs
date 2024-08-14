using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common;
using tph.ChainedTurtles.Common.DTO;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.ChainedTurtles.BusinessEntities
{
    //LONG Signal on downside breakthroughs
    //SHORT Signal on upside breakthroughs
    public class MonChainedInvBOBTurtleIndicator : MonChainedTurtleIndicator, ITrendlineIndicator
    {
        #region Constructor 

        public MonChainedInvBOBTurtleIndicator(Security pSecurity,
                                               TurtlesCustomConfig pTurtlesCustomConfig,
                                               string pCode,
                                               ILogger pLogger) :base(pSecurity,pTurtlesCustomConfig,pCode)
        {
            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);
        }

        #endregion

        #region Protected Attributes

        protected int InnerTrendlinesSpan { get;set; }


        protected int OutterTrendlinesSpan { get; set; }

        protected double PerforationThreshold { get; set; }

        protected string CandleReferencePrice { get;set; }

        protected bool RecalculateTrendlines { get; set; }

        protected int HistoricalPricesPeriod { get; set; }

        protected int SkipCandlesToBreakTrndln { get; set; }

        #endregion



        #region Protected Methods

        private void LoadConfigValues(string customConfig)
        {
            //
            try
            {
                TrendlineTurtleIndicatorConfigDTO resp = JsonConvert.DeserializeObject<TrendlineTurtleIndicatorConfigDTO>(customConfig);


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


                if (!string.IsNullOrEmpty(resp.closingTime))
                {
                    EvalTime(resp.closingTime);
                    ClosingTime = resp.closingTime;
                }
                else
                    throw new Exception("Missing config value closingTime");


                if (resp.innerTrendlinesSpan > 0)
                    InnerTrendlinesSpan = resp.innerTrendlinesSpan;
                else
                    throw new Exception("config value innerTrendlinesSpan must be greater than 0");


                if (resp.outterTrendlinesSpan > 0)
                    OutterTrendlinesSpan = resp.outterTrendlinesSpan;
                else
                    throw new Exception("config value outterTrendlinesSpan must be greater than 0");


                if (resp.perforationThresholds >= 0)
                    PerforationThreshold = resp.perforationThresholds;
                else
                    throw new Exception("config value perforationThresholds must be greater or equal than 0");



                if (!string.IsNullOrEmpty(resp.candleReferencePrice))
                {
                    
                    CandleReferencePrice = resp.candleReferencePrice;
                }
                else
                    throw new Exception("Missing config value candleReferencePrice");

                if (resp.recalculateTrendlines.HasValue)
                    RecalculateTrendlines = resp.recalculateTrendlines.Value;
                else
                    RecalculateTrendlines = true;



                if (resp.historicalPricesPeriod <= 0)
                    HistoricalPricesPeriod = resp.historicalPricesPeriod;
                else
                    throw new Exception("config value historicalPricesPeriod must be lower than 0");

                if (resp.skipCandlesToBreakTrndln >= 0)
                    SkipCandlesToBreakTrndln = resp.skipCandlesToBreakTrndln;
                else
                    throw new Exception("config value skipCandlesToBreakTrndln must be greater or equal than 0");


            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error deserializing custom config for symbol {Security.Symbol}:{ex.Message} ");
            }
        }

        #endregion

        #region Public Overriden Methods


        //Activates the signal indicator or other statistical calculations depending the monitoring position role in the trading strategy
        public override bool EvalSignalTriggered()
        {
            bool longSignal=LongSignalTriggered();
            bool shortSignal= ShortSignalTriggered();

            return longSignal || shortSignal;

        }

        public override bool LongSignalTriggered()
        {
            if (!LongSignalOn)
            {
                if (DownsideBreaktrhough())
                {
                    LongSignalOn = true;
                    LastSignalTimestamp = DateTimeManager.Now;
                    return LongSignalOn;
                }
                else
                {
                    return false;

                }
            }
            else
            {
                EvalTimestampExpiration();
                return LongSignalOn;
            }
        }

        public override bool ShortSignalTriggered()
        {

            if (!ShortSignalOn)
            {
                if (UpsideBreaktrhough())
                {
                    ShortSignalOn = true;
                    LastSignalTimestamp = DateTimeManager.Now;
                    return ShortSignalOn;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                EvalTimestampExpiration();
                return ShortSignalOn;
            }
        }


        public override bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
            bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
            if (!higherMMov)
            {
                LastSignalTriggered = $"CLOSE SHORT w/MMov : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
                return false;
        }

        public override bool EvalClosingLongPosition(PortfolioPosition portfPos)
        {
            bool higherMMov = IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
            if (higherMMov)
            {
                LastSignalTriggered = $"CLOSE LONG w/Turtles : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
            {

                return false;
            }
        }




        #endregion

        #region ITrendlineIndicator

        public override bool IsTrendlineMonPosition()
        {
            return true;
        }

        public int GetInnerTrendlinesSpan()
        {
            return InnerTrendlinesSpan;
        }

        public int GetOutterTrendlinesSpan()
        {
            return OutterTrendlinesSpan;
        }

        public double GetPerforationThreshold()
        {
            return PerforationThreshold;
        }

        public string GetCandleReferencePrice()
        {
            return CandleReferencePrice;
        }

        public bool GetRecalculateTrendlines()
        {
            return RecalculateTrendlines;
        }


        public int GetHistoricalPricesPeriod()
        {
            return HistoricalPricesPeriod;

        }

        public int GetSkipCandlesToBreakTrndln()
        { 
            return SkipCandlesToBreakTrndln;
        
        }

        #endregion
    }
}

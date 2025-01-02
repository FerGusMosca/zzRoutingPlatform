using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Util;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Util;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedTurtleIndicator : MonTrendlineTurtlesPosition
    {

        #region Public Attributes

        public string Code { get; set; }

        protected bool LongSignalOn { get; set; }

        protected bool ShortSignalOn { get; set; }

        protected DateTime? LastSignalTimestamp { get; set; }

        protected int InnerTrendlinesSpan { get; set; }


        protected double PerforationThreshold { get; set; }

        //protected string CandleReferencePrice { get; set; }

        protected bool RecalculateTrendlines { get; set; }

        protected int HistoricalPricesPeriod { get; set; }

        protected int SkipCandlesToBreakTrndln { get; set; }

        public bool RequestHistoricalPrices{ get; set; }

        public bool IsSingleSecurityIndicator { get; set; }

        #endregion

        #region Protected Consts

        public static string _BOB_SIGNAL_TYPE = "BOB";
        public static string _BOB_INV_SIGNAL_TYPE = "BOB_INV";
        public static string _MULT_SYMBOL_INDICATOR = "MULT_SYMBOL_INDICATOR";

        protected static int _SIGNAL_EXPIRATION_IN_MIN = 5;

        #endregion


        #region Constructor 

        public MonChainedTurtleIndicator(Security pSecurity, TurtlesCustomConfig pTurtlesCustomConfig,
                                        string pCode,bool pIsSingleSecurityIndicator=true) :base(pTurtlesCustomConfig, 0,null,null)
        {


            Security = pSecurity;
            Code = pCode;

            LongSignalOn = false;
            ShortSignalOn = false;
            LastSignalTimestamp = null;
            IsSingleSecurityIndicator = pIsSingleSecurityIndicator;

        }

        #endregion


        #region Protected Methods

        protected void LoadConfigValues(string customConfig)
        {
            //
            try
            {
                TrendlineTurtleIndicatorConfigDTO resp = JsonConvert.DeserializeObject<TrendlineTurtleIndicatorConfigDTO>(customConfig);

                MarketStartTime = TurtleIndicatorBaseConfigLoader.GetMarketStartTime(  resp);
                MarketEndTime = TurtleIndicatorBaseConfigLoader.GetMarketEndTime(resp);
                ClosingTime = TurtleIndicatorBaseConfigLoader.GetClosingTime(resp);
                HistoricalPricesPeriod = TurtleIndicatorBaseConfigLoader.GetHistoricalPricesPeriod(resp);
                RequestHistoricalPrices = TurtleIndicatorBaseConfigLoader.GetRequestHistoricalPrices(resp, true);


                if (resp.innerTrendlinesSpan > 0)
                    InnerTrendlinesSpan = resp.innerTrendlinesSpan;
                else
                    throw new Exception("config value innerTrendlinesSpan must be greater than 0");


                if (resp.outterTrendlinesSpan > 0)
                    OuterSignalSpan = resp.outterTrendlinesSpan;
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

        public void EvalTimestampExpiration()
        {

            if (LastSignalTimestamp.HasValue)
            {
                TimeSpan elapsed = DateTimeManager.Now - LastSignalTimestamp.Value;

                if (elapsed.TotalMinutes > _SIGNAL_EXPIRATION_IN_MIN)
                {
                    LastSignalTimestamp = null;
                    LongSignalOn = false;
                    ShortSignalOn = false;
                }
            }

        }

        public bool DownsideBreaktrhough()
        {
            if(LastValidCandle()!=null)
                DoLog($"DBG5d-MMov={CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)} Candle={LastValidCandle().Trade}", Constants.MessageType.Information);

            DoLog($"DBG9d-CandleReferencePrice={CandleReferencePrice} CloseWindow={TurtlesCustomConfig.CloseWindow}", Constants.MessageType.Information);
            return EvalSupportBroken() && !IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
        }

        public bool UpsideBreaktrhough()
        {
            if(LastValidCandle()!=null)
                DoLog($"DBG5u-MMov={CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)} Candle={LastValidCandle().Trade}", Constants.MessageType.Information);

            DoLog($"DBG9u-CandleReferencePrice={CandleReferencePrice} CloseWindow={TurtlesCustomConfig.CloseWindow}", Constants.MessageType.Information);
            return EvalResistanceBroken() && IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
        }


        #endregion
    }
}

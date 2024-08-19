using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.ChainedTurtles.Common.DTO;
using tph.ChainedTurtles.Common.Interfaces;
using tph.ChainedTurtles.Common.Util;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Interfaces;

namespace tph.ChainedTurtles.BusinessEntities
{
    public class MonChainedMovAvgTurtleIndicator : MonChainedTurtleIndicator, ITradingEnity
    {
        #region Constructor 

        public MonChainedMovAvgTurtleIndicator(Security pSecurity,
                                                  TurtlesCustomConfig pTurtlesCustomConfig,
                                                    string pCode,
                                                   ILogger pLogger) : base(pSecurity, pTurtlesCustomConfig, pCode)
        {
            LoadConfigValues(pTurtlesCustomConfig.CustomConfig);
        }

        #endregion

        #region Protected Attributes

        protected int AvgPeriod { get; set; }

        protected string MovAvgSignalTriggered { get; set; }

        #endregion


        #region Private Methods

        private void LoadConfigValues(string pCustomConfig)
        {
            //
            try
            {
                MovAvgTurtleIndicatorConfigDTO resp = JsonConvert.DeserializeObject<MovAvgTurtleIndicatorConfigDTO>(pCustomConfig);

                MarketStartTime = TurtleIndicatorBaseConfigLoader.GetMarketStartTime(resp);
                MarketEndTime = TurtleIndicatorBaseConfigLoader.GetMarketEndTime(resp);
                ClosingTime = TurtleIndicatorBaseConfigLoader.GetClosingTime(resp);
                HistoricalPricesPeriod = TurtleIndicatorBaseConfigLoader.GetHistoricalPricesPeriod(resp,-1);
                RequestHistoricalPrices = TurtleIndicatorBaseConfigLoader.GetRequestHistoricalPrices(resp, true);

                if (resp.avgPeriod > 0)
                    AvgPeriod = resp.avgPeriod;
                else
                    throw new Exception("config value avgPeriod must be greater than 0");
            }
            catch (Exception ex)
            {
                throw new Exception($"CRITICAL error deserializing custom config for symbol {Security.Symbol}:{ex.Message} ");
            }


        }

        #endregion

        #region Base Overriden Methods


        public override bool EvalSignalTriggered()
        {
            bool longSignal = LongSignalTriggered();
            bool shortSignal = ShortSignalTriggered();

            return longSignal || shortSignal;

        }


        public override bool LongSignalTriggered()
        {
            bool higherMMov = IsHigherThanMMov(AvgPeriod, false);
            if (higherMMov)
            {
                MovAvgSignalTriggered = $"LONG SIGNAL w/MMOV for indicator for security {Security.Symbol} : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
                return false;
        }


        public override bool ShortSignalTriggered()
        {
           
            bool lowerMMov = !IsHigherThanMMov(AvgPeriod, false);
            if (lowerMMov)
            {
                MovAvgSignalTriggered = $"SHORT SIGNAL w/MMOV for indicator for security {Security.Symbol} : Last Candle:{ReferencePriceCalculator.GetReferencePrice(GetLastFinishedCandle(), CandleReferencePrice)} MMov:{CalculateSimpleMovAvg(TurtlesCustomConfig.CloseWindow)}";
                return true;
            }
            else
                return false;

        }


        public override string SignalTriggered()
        {
            return MovAvgSignalTriggered;
        }

        public string GetCandleReferencePrice()
        {
            return CandleReferencePrice;
        }

        public int GetHistoricalPricesPeriod()
        {
            return HistoricalPricesPeriod;
        }


        //EvalClosingShortPosition --> Uses standard closing mechanism
        //EvalClosingLongPosition -->  Uses standard closing mehcanism

        #endregion
    }
}

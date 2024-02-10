using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using tph.StrategyHandler.HistoricalPricesAnalyzer.Common.DTOs;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.StrategyHandler.HistoricalPricesAnalyzer.BE
{
    public class NPeriodCandleIndicator : Indicator
    {

        #region Protected Attributes

        protected NPeriodCandleConfig Config { get; set; }

        #endregion

        #region Constructor

        public NPeriodCandleIndicator(Security pSecurity, string pConfigFile) : base()
        {

            Security = pSecurity;

            Config = LoadConfigDTO<NPeriodCandleConfig>(OpenConigFile(pConfigFile));

            IndicatorClassifKey = string.Format(Config.Key,Config.Period, pSecurity.Symbol);

            DateRangeClassifications = new List<DateRangeClassification>();
        }


        #endregion

        #region Protected Methods

        protected MarketData GetPrevDayOfWeek(int periodSkip,DayOfWeek dayOfWeek,MarketData currMarketData)
        {
            if (periodSkip < 0)
                throw new Exception($"Invalid value for period skip!");

            int i = 0;
            MarketData lastMD = null;
            foreach (MarketData candle in Candles.Values.OrderByDescending(x => x.GetReferenceDateTime()))
            {

                if (candle.GetReferenceDateTime().Value.DayOfWeek == dayOfWeek
                    && DateTime.Compare(candle.GetReferenceDateTime().Value,currMarketData.GetReferenceDateTime().Value)!=0)
                    i += 1;

                lastMD = candle;

                if(i == periodSkip +1)
                {
                    return lastMD;
                }
            }

            return lastMD;
        }


        #endregion

        #region Overriden Methods

        public override bool LongSignalTriggered()
        {
            if (Candles.Count <= (Config.Period*7) * 2)//We need 2 weeks at least
                return false;

            MarketData currMarketData = GetLastFinishedCandle(0);
            if (currMarketData.GetReferenceDateTime().Value.DayOfWeek.ToString() != Config.RecalculationDay)
                return false;

            MarketData tlastFriday = GetPrevDayOfWeek(Config.Period-1,DayOfWeek.Friday,currMarketData);
            MarketData tlastMonday = GetPrevDayOfWeek(Config.Period-1,DayOfWeek.Monday, currMarketData);


            MarketData tprevFriday = GetPrevDayOfWeek(Config.Period, DayOfWeek.Friday, currMarketData);
            MarketData tprevMonday = GetPrevDayOfWeek(Config.Period, DayOfWeek.Monday, currMarketData);

            double triggerPointDelta = Math.Abs(tprevMonday.OpeningPrice.Value - tprevFriday.ClosingPrice.Value) / 2;
            double offset = tprevMonday.OpeningPrice > tprevFriday.ClosingPrice ? tprevFriday.ClosingPrice.Value : tprevMonday.OpeningPrice.Value;

            double refPrice = offset + triggerPointDelta;

            return tlastFriday.ClosingPrice < refPrice;
        }

        public override bool ShortSignalTriggered()
        {

            if (Candles.Count <= (Config.Period*7) * 2)//We need 2 weeks at least
                return false;

            MarketData currMarketData = GetLastFinishedCandle(0);
            if (currMarketData.GetReferenceDateTime().Value.DayOfWeek.ToString() != Config.RecalculationDay)
                return false;

            MarketData tlastFriday = GetPrevDayOfWeek(Config.Period-1, DayOfWeek.Friday, currMarketData);
            MarketData tlastMonday = GetPrevDayOfWeek(Config.Period-1, DayOfWeek.Monday, currMarketData);


            MarketData tprevFriday = GetPrevDayOfWeek(Config.Period, DayOfWeek.Friday, currMarketData);
            MarketData tprevMonday = GetPrevDayOfWeek(Config.Period, DayOfWeek.Monday, currMarketData);

            double triggerPointDelta = Math.Abs(tprevMonday.OpeningPrice.Value - tprevFriday.ClosingPrice.Value) / 2;
            double offset = tprevMonday.OpeningPrice > tprevFriday.ClosingPrice ? tprevFriday.ClosingPrice.Value : tprevMonday.OpeningPrice.Value;

            double refPrice = offset + triggerPointDelta;

            return tlastFriday.ClosingPrice > refPrice;
        }

        public override bool EvalClosingLongPosition(PortfolioPosition portfPos)
        {
            //TODO eval closing LONG position if implemented
            return false;
        }

        public override bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
            //TODO eval closing SHORT position if implemented
            return false;
        }

        public override bool EvalClosingOnTargetPct(PortfolioPosition portfPos)
        {
            return false;
        }

        public override bool EvalStopLossHit(PortfTurtlesPosition tradPos)
        {
            return false;
        }

        public override bool EvalAbortingClosingLongPosition()
        {
            return false;
        }


        public override bool EvalAbortingClosingShortPosition()
        {
            return false;
        }

        public override bool AppendCandle(MarketData md)
        {

            if (md.GetReferenceDateTime().HasValue)
            {
                AppendCandleLight(md);
                if (LastOpenedClassification== null || LastOpenedClassification.IsLongClassif())
                {
                    if (ShortSignalTriggered())
                        if (LastOpenedClassification != null)
                            SwitchRangeClassification(GetLastFinishedCandle(0).GetReferenceDateTime().Value, md.GetReferenceDateTime().Value, DateRangeClassification._SHORT_CLASSIF);
                        else
                            OpenDateRangeClassification(md.GetReferenceDateTime().Value, DateRangeClassification._SHORT_CLASSIF);

                }
                else if (LastOpenedClassification == null || LastOpenedClassification.IsShortClassif())
                {
                    if (LongSignalTriggered())
                        if (LastOpenedClassification != null)
                            SwitchRangeClassification(GetLastFinishedCandle(0).GetReferenceDateTime().Value, md.GetReferenceDateTime().Value, DateRangeClassification._LONG_CLASSIF);
                        else
                            OpenDateRangeClassification(md.GetReferenceDateTime().Value, DateRangeClassification._LONG_CLASSIF);
                }
                else
                    throw new Exception($"Could not find the right classification for the last range found : {LastOpenedClassification.Classification} " +
                        $"for symbol {Security.Symbol} on Date {md.GetReferenceDateTime()}");
                return true;
            }
            else
                return false;
        }


        #endregion
    }
}

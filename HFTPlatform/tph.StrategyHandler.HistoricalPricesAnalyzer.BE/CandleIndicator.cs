using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.StrategyHandler.HistoricalPricesAnalyzer.BE
{
    public class CandleIndicator: Indicator
    {
        #region Constructor

        public CandleIndicator(Security pSecurity):base() 
        { 
        
            Security= pSecurity;

            IndicatorClassifKey = $"CANDLE_CLASSIF_DAILY_{pSecurity.Symbol}";

            DateRangeClassifications = new List<DateRangeClassification>();



        }


        #endregion

        #region Protected Methods

        public void OpenDateRangeClassification(DateTime date,string classif)
        {
            DateRangeClassification rangeClassif = new DateRangeClassification()
            {
                Key = IndicatorClassifKey,
                DateStart = date,
                Classification = classif,
            };

            LastOpenedClassification = rangeClassif;

            DateRangeClassifications.Add(rangeClassif);
        
        }

        public void SwitchRangeClassification(DateTime closeDate,DateTime newOpenDate, string newClassif)
        {
            LastOpenedClassification.DateEnd=closeDate;

            DateRangeClassification newRangeClassif = new DateRangeClassification()
            {
                Key = IndicatorClassifKey,
                DateStart = newOpenDate,
                Classification = newClassif,
            };

            DateRangeClassifications.Add(newRangeClassif);

            LastOpenedClassification = newRangeClassif;

        }

        #endregion

        #region Overriden Methods

        public override bool LongSignalTriggered()
        {
            MarketData tMinus2 = GetLastFinishedCandle(1);
            MarketData tMinus1 = GetLastFinishedCandle(0);

            if (tMinus1 != null && tMinus2 != null && tMinus2.OpeningPrice.HasValue && tMinus2.ClosingPrice.HasValue)
            {

                double triggerPointDelta = Math.Abs(tMinus2.OpeningPrice.Value - tMinus2.ClosingPrice.Value)/2;
                double offset = tMinus2.OpeningPrice > tMinus2.ClosingPrice ? tMinus2.ClosingPrice.Value : tMinus2.OpeningPrice.Value;

                double refPrice = offset + triggerPointDelta;

                return tMinus1.ClosingPrice > refPrice;


            }
            else
                return false;
        }

        public override bool ShortSignalTriggered()
        {

            MarketData tMinus2 = GetLastFinishedCandle(1);
            MarketData tMinus1 = GetLastFinishedCandle(0);

            if (tMinus1 != null && tMinus2 != null && tMinus2.OpeningPrice.HasValue && tMinus2.ClosingPrice.HasValue)
            {

                double triggerPointDelta = Math.Abs(tMinus2.OpeningPrice.Value - tMinus2.ClosingPrice.Value)/2;
                double offset = tMinus2.OpeningPrice < tMinus2.ClosingPrice ? tMinus2.OpeningPrice.Value : tMinus2.ClosingPrice.Value;

                double refPrice = offset + triggerPointDelta;

                return tMinus1.ClosingPrice < refPrice;


            }
            else
                return false;
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

                if (LastOpenedClassification==null)
                {
                    if (md.BiggerGreendCandle(int.MinValue) )
                    {
                        OpenDateRangeClassification(md.GetReferenceDateTime().Value, DateRangeClassification._LONG_CLASSIF);
                    }
                    else if (md.LowerRedCandle(int.MaxValue))
                    {
                        OpenDateRangeClassification(md.GetReferenceDateTime().Value, DateRangeClassification._SHORT_CLASSIF);

                    }
                }
                else
                {
                    if (LastOpenedClassification.IsLongClassif())
                    {
                        if (ShortSignalTriggered())
                            SwitchRangeClassification(GetLastFinishedCandle(0).GetReferenceDateTime().Value,md.GetReferenceDateTime().Value, DateRangeClassification._SHORT_CLASSIF);
                        

                    }
                    else if (LastOpenedClassification.IsShortClassif())
                    {
                        if (LongSignalTriggered())
                            SwitchRangeClassification(GetLastFinishedCandle(0).GetReferenceDateTime().Value, md.GetReferenceDateTime().Value, DateRangeClassification._LONG_CLASSIF);
                    }
                    else
                        throw new Exception($"Could not find the right classification for the last range found : {LastOpenedClassification.Classification} " +
                            $"for symbol {Security.Symbol} on Date {md.GetReferenceDateTime()}");
                }

                string key = GetCandleKey(md);
                Candles[key]=md;
                return true;
            }
            else
                return false;
        }


        #endregion
    }
}

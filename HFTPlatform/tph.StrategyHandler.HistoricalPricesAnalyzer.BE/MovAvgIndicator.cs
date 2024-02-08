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
    public class MovAvgIndicator: Indicator
    {

        #region Protected Attributes

        protected MovAvgIndicatorConfig Config { get; set; }

        #endregion

        #region Constructor

        public MovAvgIndicator(Security pSecurity, string pConfigFile) : base()
        {

            Security = pSecurity;

            Config = LoadConfigDTO<MovAvgIndicatorConfig>(OpenConigFile(pConfigFile));

            IndicatorClassifKey = string.Format(Config.Key, pSecurity.Symbol);

            DateRangeClassifications = new List<DateRangeClassification>();

        }


        #endregion

        #region Overriden Methods

        public override bool LongSignalTriggered()
        {

            return IsHigherThanMMov(Config.Window, true);
        }

        public override bool ShortSignalTriggered()
        {

            return !IsHigherThanMMov(Config.Window, false);
        }

        public override bool EvalClosingLongPosition(PortfolioPosition portfPos)
        {
            return false;
        }

        public override bool EvalClosingShortPosition(PortfolioPosition portfPos)
        {
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
                if (LastOpenedClassification == null)
                {
                    OpenSemiRandomInitialPos(md);
                }
                else
                {
                    if (LastOpenedClassification.IsLongClassif())
                    {
                        if (ShortSignalTriggered())
                            SwitchRangeClassification(GetLastFinishedCandle(0).GetReferenceDateTime().Value, md.GetReferenceDateTime().Value, DateRangeClassification._SHORT_CLASSIF);


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

                return true;
            }
            else
                return false;
        }


        #endregion
    }
}

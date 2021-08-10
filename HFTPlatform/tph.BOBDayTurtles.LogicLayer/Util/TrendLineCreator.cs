using System;
using System.Collections.Generic;
using System.Linq;
using tph.BOBDayTurtles.BusinessEntities;
using tph.BOBDayTurtles.Common.Configuration;
using tph.BOBDayTurtles.Common.Util;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.BOBDayTurtles.LogicLayer.Util
{
    public class TrendLineCreator
    {
        #region Private Attributes

        private Security Stock { get; set; }

        private StrategyConfiguration StrategyConfiguration { get; set; }
        
        protected CandleInterval CandleInterval{ get; set; }

        //private TrendlineConfiguration TrendlineConfiguration { get; set; }

        public List<MarketData> LocalMinimums { get; set; }

        public List<MarketData> LocalMaximums { get; set; }

        public  List<Trendline> SupportTrendlines { get; set; }

        public List<Trendline> ResistanceTrendlines { get; set; }

        #endregion

        #region Constructor

        public TrendLineCreator(Security pStock,StrategyConfiguration pConfig,CandleInterval pInterval)
        {
            Stock = pStock;

            CandleInterval = pInterval;

            StrategyConfiguration = pConfig;

            //TrendlineConfiguration = pTrendlineConf;

            LocalMinimums = new List<MarketData>();

            LocalMaximums = new List<MarketData>();

            SupportTrendlines = new List<Trendline>();

            ResistanceTrendlines = new List<Trendline>();

        }
        
        public TrendLineCreator(Security pStock, StrategyConfiguration pConfig,CandleInterval pInterval, List <Trendline> supports, List<Trendline> resistances)
        {
            Stock = pStock;

            StrategyConfiguration = pConfig;
            
            CandleInterval = pInterval;

            LocalMinimums = new List<MarketData>();

            LocalMaximums = new List<MarketData>();

            SupportTrendlines = new List<Trendline>();
            
            if(supports!=null)
                SupportTrendlines.AddRange(supports);

            ResistanceTrendlines = new List<Trendline>();

            if(resistances!=null)
                ResistanceTrendlines.AddRange(resistances);

        }


        #endregion


        #region Protected Methods

        private DateTime GetStartDate(DateTime date, int span)
        {
            
            if (CandleInterval == CandleInterval.Minute_1)
                return date.AddMinutes(span);
            else if (CandleInterval == CandleInterval.HOUR_1)
                return date.AddHours(span);
            else if (CandleInterval == CandleInterval.DAY)
                return date.AddDays(span);
            else
            {
                throw new Exception(string.Format("Trendline Creator.GetStartDate - Candle Interval not implemented:{0}",CandleInterval));
            }
        }
        
        private DateTime GetEndDate(DateTime date, int span)
        {
            
            if (CandleInterval == CandleInterval.Minute_1)
                return date.AddMinutes(-1*span);
            else if (CandleInterval == CandleInterval.HOUR_1)
                return date.AddHours(-1*span);
            else if (CandleInterval == CandleInterval.DAY)
                return date.AddDays(-1*span);
            else
            {
                throw new Exception(string.Format("Trendline Creator.GetEndDate - Candle Interval not implemented:{0}",CandleInterval));
            }
        }
        
        private int GetSpan(DateTime start, DateTime end)
        {

            if (CandleInterval == CandleInterval.Minute_1)
                return Convert.ToInt32((end - start).TotalMinutes);
            else if (CandleInterval == CandleInterval.HOUR_1)
                return Convert.ToInt32((end - start).TotalHours);
            else if (CandleInterval == CandleInterval.DAY)
                return Convert.ToInt32((end - start).TotalDays);
            else
            {
                throw new Exception(string.Format("Trendline Creator.GetSpan - Candle Interval not implemented:{0}",
                    CandleInterval));
            }
        }

        private bool EvalTimespan(MarketData newLocalMinimum,MarketData localMimimum)
        {

            if (CandleInterval == CandleInterval.Minute_1)
                return ((newLocalMinimum.MDEntryDate.Value - localMimimum.MDEntryDate.Value).TotalMinutes >
                        Convert.ToDouble(StrategyConfiguration.InnerTrendlinesSpan));
            else if (CandleInterval == CandleInterval.HOUR_1)
                return ((newLocalMinimum.MDEntryDate.Value - localMimimum.MDEntryDate.Value).TotalHours >
                        Convert.ToDouble(StrategyConfiguration.InnerTrendlinesSpan));
            else if (CandleInterval == CandleInterval.DAY)
                return ((newLocalMinimum.MDEntryDate.Value - localMimimum.MDEntryDate.Value).TotalDays >
                    Convert.ToDouble(StrategyConfiguration.InnerTrendlinesSpan));
            else
            {
                throw new Exception(string.Format("Trendline Creator - Candle Interval not implemented:{0}",CandleInterval));
            }
        }

        protected bool EvalLocalMinimum(List<MarketData> prices, MarketData currentPrice,int index)
        {
            int startIndex = index - StrategyConfiguration.InnerTrendlinesSpan;
            int endIndex = index + StrategyConfiguration.InnerTrendlinesSpan;

            int take = endIndex-startIndex;
            List<MarketData> rangePrices = prices.Skip(startIndex).Take(take).ToList();

            return !rangePrices.Any(x => GetLowestPrice(x) < GetLowestPrice(currentPrice)) && (rangePrices.Count == take);

        }

        protected double GetHighestPrice(MarketData md)
        {
            if (md.ClosingPrice > md.OpeningPrice)
                return md.ClosingPrice.Value;
            else
                return md.OpeningPrice.Value;
        }
        
        protected double GetLowestPrice(MarketData md)
        {
            if (md.ClosingPrice < md.OpeningPrice)
                return md.ClosingPrice.Value;
            else
                return md.OpeningPrice.Value;
        }

        protected bool EvalLocalMaximum(List<MarketData> prices, MarketData currentPrice, int index)
        {
            int startIndex = index - StrategyConfiguration.InnerTrendlinesSpan;
            int endIndex = index + StrategyConfiguration.InnerTrendlinesSpan;

            int take = endIndex - startIndex;
            List<MarketData> rangePrices = prices.Skip(startIndex).Take(take).ToList();

            List<MarketData> temp = rangePrices.Where(x => GetHighestPrice(x) > GetHighestPrice(currentPrice)).ToList();

            return !rangePrices.Any(x => GetHighestPrice(x) > GetHighestPrice(currentPrice)) && (rangePrices.Count == take);

        }

        protected bool EvalTrendlineBroken(MarketData price, List<MarketData> allHistoricalPrices, Trendline possibleTrendline, bool downside)
        {

            double trendlinePrice = possibleTrendline.CalculateTrendPrice(price.MDEntryDate.Value, allHistoricalPrices);

            if (downside)
            {
                trendlinePrice = trendlinePrice - (StrategyConfiguration.PerforationThresholds * trendlinePrice);

                if (GetLowestPrice(price) < trendlinePrice)
                    return true;
                else
                    return false;
            }
            else
            {
                trendlinePrice = trendlinePrice + (StrategyConfiguration.PerforationThresholds * trendlinePrice);

                if (GetHighestPrice(price)> trendlinePrice)
                    return true;
                else
                    return false;
            }
        }

        protected bool EvalTrendlineBroken(List<MarketData> allHistoricalPrices, Trendline possTrnd,bool downside)
        {
            //We just consider the prices outside of the local maximum/minimum range
            DateTime startDate = GetStartDate(possTrnd.StartDate,StrategyConfiguration.InnerTrendlinesSpan);
            DateTime endDate = GetEndDate(possTrnd.EndDate,StrategyConfiguration.InnerTrendlinesSpan);

            IList<MarketData> pricesInPeriod = allHistoricalPrices
                .Where(x => DateTime.Compare(x.MDEntryDate.Value, startDate) >= 0
                            && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToList();

            foreach (MarketData price in pricesInPeriod)
            {

                if (EvalTrendlineBroken(price, allHistoricalPrices, possTrnd, downside))
                    return true;
            }


            return false;
        }

        protected void EvalSupport(Security stock,MarketData newLocalMinimum, IList<Trendline> trendlines,List<MarketData> allHistoricalPrices)
        {
            foreach (MarketData localMimimum in LocalMinimums)
            {
                if (EvalTimespan(newLocalMinimum,localMimimum))
                {
                    Trendline possibleTrendline = new Trendline()
                    {
                        JustBroken = false,
                        Security = stock,
                        StartPrice = localMimimum,
                        EndPrice = newLocalMinimum,
                        BrokenDate = null,
                        TrendlineType = TrendlineType.Support,
                        //TrendlineConfiguration = TrendlineConfiguration,
                        Modified = true
                    };

                    if (!EvalTrendlineBroken(allHistoricalPrices, possibleTrendline, true))
                    {
                        trendlines.Add(possibleTrendline);
                    }
                }
            }
        }

        protected void EvalResistance(Security stock, MarketData newLocalMaximum, IList<Trendline> trendlines, List<MarketData> allHistoricalPrices)
        {
            foreach (MarketData localMaximum in LocalMaximums)
            {
                if (EvalTimespan(newLocalMaximum,localMaximum))
                {
                    Trendline possibleTrendline = new Trendline()
                    {
                        JustBroken = false,
                        Security = stock,
                        StartPrice = localMaximum,
                        EndPrice = newLocalMaximum,
                        BrokenDate = null,
                        TrendlineType = TrendlineType.Resistance,
                        //TrendlineConfiguration = TrendlineConfiguration,
                        Modified = true
                    };

                    if (!EvalTrendlineBroken(allHistoricalPrices, possibleTrendline, false))
                    {
                        trendlines.Add(possibleTrendline);
                    }
                }
            }
        }

        protected void EvalBrokenTrendlines(MarketData price,List<Trendline> trendlines, List<MarketData> allHistoricalPrices, bool downside)
        {
            
            foreach (Trendline trendline in trendlines.Where(x=>   x.BrokenDate==null 
                                                                && DateTime.Compare(x.EndDate,price.MDEntryDate.Value)<0).ToList())
            {
                if (GetSpan(trendline.EndDate, price.MDEntryDate.Value) > 5)//Safety threshold
                {

                    if (EvalTrendlineBroken(price, allHistoricalPrices, trendline, downside))
                    {
                        trendline.BrokenDate = price.MDEntryDate.Value;
                        trendline.Modified = true;
                    }
                }
            }
        }

        protected void ExtractPrevMinimums(bool usePrevTrendlines, DateTime? prevStartDate)
        {
            LocalMinimums.Clear();

            if (usePrevTrendlines)
            {
                if (!prevStartDate.HasValue)
                    throw new Exception("If you want to decompose prev supports you have to provide a start date");

                foreach (Trendline trendline in SupportTrendlines)
                {
                    if (DateTime.Compare(trendline.StartDate, prevStartDate.Value) >= 0 && !LocalMinimums.Contains(trendline.StartPrice))
                        LocalMinimums.Add(trendline.StartPrice);

                    if (DateTime.Compare(trendline.EndDate, prevStartDate.Value) >= 0 && !LocalMinimums.Contains(trendline.EndPrice))
                        LocalMinimums.Add(trendline.EndPrice);
                }
            
            }
        }

        protected void ExtractPrevMaximums(bool usePrevTrendlines, DateTime? prevStartDate)
        {
            LocalMaximums.Clear();

            if (usePrevTrendlines)
            {
                if (!prevStartDate.HasValue)
                    throw new Exception("If you want to decompose prev supports you have to provide a start date");

                foreach (Trendline trendline in ResistanceTrendlines)
                {
                    if (DateTime.Compare(trendline.StartDate, prevStartDate.Value) >= 0 && !LocalMinimums.Contains(trendline.StartPrice))
                        LocalMaximums.Add(trendline.StartPrice);

                    if (DateTime.Compare(trendline.EndDate, prevStartDate.Value) >= 0 && !LocalMinimums.Contains(trendline.EndPrice))
                        LocalMaximums.Add(trendline.EndPrice);
                }
            }
        }

        #endregion

        #region Public Methods

        public List<Trendline> ProcessSupportTrendlines(Security stock, List<MarketData> allHistoricalPrices,bool processSafetyThreshold,
                                                        DateTime startDate,DateTime endDate,bool usePrevTrendlines,DateTime? startPrevTrendlineDate)
        {
            int i = 0;
            ExtractPrevMinimums(usePrevTrendlines, startPrevTrendlineDate);

            MarketData[] pricesArr = allHistoricalPrices.Where(x => DateTime.Compare(startDate, x.MDEntryDate.Value) <= 0
                                                               && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToArray();

            foreach (MarketData price in pricesArr)
            {
                if (i < StrategyConfiguration.InnerTrendlinesSpan && processSafetyThreshold)
                {
                    i++;
                    continue;
                }


                if (EvalLocalMinimum(pricesArr.ToList(), price, i))
                {

                    EvalSupport(stock, price, SupportTrendlines, allHistoricalPrices);

                    LocalMinimums.Add(price);
                }

                EvalBrokenTrendlines(price, SupportTrendlines, allHistoricalPrices, true);

                i++;
            }

            return SupportTrendlines;

        }

        public List<Trendline> ProcessResistanceTrendlines(Security stock, List<MarketData> allHistoricalPrices, bool processSafetyThreshold,
                                                           DateTime startDate, DateTime endDate,bool usePrevTrendlines,DateTime? startPrevTrendlineDate)
        {
            int i = 0;
            ExtractPrevMaximums(usePrevTrendlines, startPrevTrendlineDate);

            MarketData[] pricesArr = allHistoricalPrices.Where(x =>   DateTime.Compare(startDate, x.MDEntryDate.Value) <= 0 
                                                               && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToArray();


            foreach (MarketData price in pricesArr)
            {
                if (i < StrategyConfiguration.InnerTrendlinesSpan && processSafetyThreshold)
                {
                    i++;
                    continue;
                }

                if (EvalLocalMaximum(pricesArr.ToList(), price, i))
                {
                    EvalResistance(stock, price, ResistanceTrendlines, allHistoricalPrices);

                    LocalMaximums.Add(price);
                }

                EvalBrokenTrendlines(price, ResistanceTrendlines, allHistoricalPrices, false);

                i++;
            }

            return ResistanceTrendlines;

        }
        
        #region Static Methods
        public static List<Trendline> BuildResistances(Security sec, List<MarketData> prices,Configuration config)
        {

            TrendLineCreator trdCreator = new TrendLineCreator(sec,
                new StrategyConfiguration()
                {
                    PerforationThresholds = config.PerforationThresholds,
                    InnerTrendlinesSpan = config.InnerTrendlinesSpan
                }, CandleInterval.Minute_1);

            DateTime minDate = prices.OrderBy(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;
            DateTime maxDate = prices.OrderByDescending(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;

            List<Trendline> trendlines = trdCreator.ProcessResistanceTrendlines(sec,
                prices, true, minDate,
                maxDate, false, null);
            return trendlines;

        }
        
        public static List<Trendline> UpdateResistances(Security sec, List<MarketData> prices,Configuration config,
                                                         List<Trendline> oldResistances,MarketData lastCandle)
        {

            TrendLineCreator trdCreator = new TrendLineCreator(sec,
                new StrategyConfiguration()
                {
                    PerforationThresholds = config.PerforationThresholds,
                    InnerTrendlinesSpan = config.InnerTrendlinesSpan
                }, CandleInterval.Minute_1, null, oldResistances);

            DateTime startDate = lastCandle.MDEntryDate.Value.AddDays(-1);
            DateTime minDate = oldResistances.OrderByDescending(x => x.EndDate).FirstOrDefault().EndDate;
            DateTime maxDate = lastCandle.MDEntryDate.Value;

            List<Trendline> trendlines = trdCreator.ProcessResistanceTrendlines(sec,
                prices, false, minDate,
                maxDate, true, startDate);
            return trendlines;

        }
        
        public static List<Trendline> UpdateSupports(Security sec, List<MarketData> prices,Configuration config,
            List<Trendline> oldSupports,MarketData lastCandle)
        {

            TrendLineCreator trdCreator = new TrendLineCreator(sec,
                new StrategyConfiguration()
                {
                    PerforationThresholds = config.PerforationThresholds,
                    InnerTrendlinesSpan = config.InnerTrendlinesSpan
                }, CandleInterval.Minute_1, oldSupports, null);

            DateTime startDate = lastCandle.MDEntryDate.Value.AddDays(-1);
            DateTime minDate = oldSupports.OrderByDescending(x => x.EndDate).FirstOrDefault().EndDate;
            DateTime maxDate = lastCandle.MDEntryDate.Value;

            List<Trendline> trendlines = trdCreator.ProcessSupportTrendlines(sec,
                                         prices, false, minDate,
                                        maxDate, true, startDate);
            return trendlines;

        }
        
        public static List<Trendline> BuildSupports(Security sec, List<MarketData> prices,Configuration config)
        {

            TrendLineCreator trdCreator = new TrendLineCreator(sec,
                new StrategyConfiguration()
                {
                    PerforationThresholds = config.PerforationThresholds,
                    InnerTrendlinesSpan = config.InnerTrendlinesSpan
                }, CandleInterval.Minute_1);

            
            DateTime minDate = prices.OrderBy(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;
            DateTime maxDate = prices.OrderByDescending(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;

            List<Trendline> trendlines = trdCreator.ProcessSupportTrendlines(sec,
                prices, true, minDate, maxDate,
                false, null);
            return trendlines;

        }
        
        #endregion
        
        #endregion
    }
}
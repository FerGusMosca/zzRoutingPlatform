using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using tph.DayTurtles.BusinessEntities;
using tph.DayTurtles.Common.Util;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Configuration;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Configuration;
using static zHFT.Main.Common.Util.Constants;

namespace tph.TrendlineTurtles.LogicLayer.Util
{
    public class TrendLineCreator
    {
        #region Protected Static Consts

        protected static int _REPEATED_MAX_MIN_MAX_DISTANCE = 10;

        protected static int _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE = 5;

        #endregion

        #region Private Static Attributes

        private static Dictionary<string,TrendLineCreator> TrdCreatorDict { get; set; }
        
        #endregion
        
        #region Private Attributes
        
        private Security Stock { get; set; }

        private TrendlineConfiguration TrendlineConfiguration { get; set; }
        
        protected CandleInterval CandleInterval{ get; set; }

        //private TrendlineConfiguration TrendlineConfiguration { get; set; }

        public List<MarketData> LocalMinimums { get; set; }

        public List<MarketData> LocalMaximums { get; set; }

        public  List<Trendline> SupportTrendlines { get; set; }
        
        public DateTime LastSafeMinDateResistances { get; set; }
        
        public DateTime LastSafeMinDateSupports{ get; set; }

        public DateTime NextPotentialMinToEval { get; set; }

        public DateTime NextPotentialMaxToEval { get; set; }

        public List<Trendline> ResistanceTrendlines { get; set; }

        protected OnLogMessage OnLogMsg { get; set; }

        #endregion

        #region Constructor

        public TrendLineCreator(Security pStock,TrendlineConfiguration pConfig,CandleInterval pInterval,
                                DateTime pMinSafeDateResistances,DateTime pMinSafeDateSupports,
                                OnLogMessage pOnLogMsg, 
                                int? _P_REPEATED_MAX_MIN_MAX_DISTANCE = null, 
                                int? _P__BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE = null)
        {
            Stock = pStock;

            CandleInterval = pInterval;

            TrendlineConfiguration = pConfig;

            //TrendlineConfiguration = pTrendlineConf;

            LocalMinimums = new List<MarketData>();

            LocalMaximums = new List<MarketData>();

            SupportTrendlines = new List<Trendline>();

            ResistanceTrendlines = new List<Trendline>();

            LastSafeMinDateResistances =pMinSafeDateResistances;

            LastSafeMinDateSupports = pMinSafeDateSupports;

            OnLogMsg += pOnLogMsg;

            if (_P_REPEATED_MAX_MIN_MAX_DISTANCE.HasValue)
                _REPEATED_MAX_MIN_MAX_DISTANCE = _P_REPEATED_MAX_MIN_MAX_DISTANCE.Value;

            if (_P__BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE.HasValue)
                _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE = _P__BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE.Value;

        }
        
        //public TrendLineCreator(Security pStock, TrendlineConfiguration pConfig,CandleInterval pInterval, List <Trendline> supports, List<Trendline> resistances)
        //{
        //    Stock = pStock;

        //    TrendlineConfiguration = pConfig;
            
        //    CandleInterval = pInterval;

        //    LocalMinimums = new List<MarketData>();

        //    LocalMaximums = new List<MarketData>();

        //    SupportTrendlines = new List<Trendline>();
            
        //    if(supports!=null)
        //        SupportTrendlines.AddRange(supports);

        //    ResistanceTrendlines = new List<Trendline>();

        //    if(resistances!=null)
        //        ResistanceTrendlines.AddRange(resistances);
            
        //    LastSafeMinDateResistances = DateTime.MinValue;

        //    LastSafeMinDateSupports = DateTime.MinValue;

        //}


        #endregion


        #region Protected Methods

        public DateTime GetStartDate(DateTime date, int span)
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
        
        private int GetSpan(DateTime start, DateTime end, List<MarketData> prices)
        {

            if (CandleInterval == CandleInterval.Minute_1)
            {
                List< MarketData> innerPrices= prices.Where(x=>DateTime.Compare(start,x.MDEntryDate.Value)<=0 
                                                            && DateTime.Compare(x.MDEntryDate.Value, end)<=0).ToList();
                return innerPrices.Count;
                //return Convert.ToInt32((end - start).TotalMinutes);
            }
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
                        Convert.ToDouble(TrendlineConfiguration.InnerTrendlinesSpan));
            else if (CandleInterval == CandleInterval.HOUR_1)
                return ((newLocalMinimum.MDEntryDate.Value - localMimimum.MDEntryDate.Value).TotalHours >
                        Convert.ToDouble(TrendlineConfiguration.InnerTrendlinesSpan));
            else if (CandleInterval == CandleInterval.DAY)
                return ((newLocalMinimum.MDEntryDate.Value - localMimimum.MDEntryDate.Value).TotalDays >
                    Convert.ToDouble(TrendlineConfiguration.InnerTrendlinesSpan));
            else
            {
                throw new Exception(string.Format("Trendline Creator - Candle Interval not implemented:{0}",CandleInterval));
            }
        }

        protected bool EvalLocalMinimum(List<MarketData> prices, MarketData currentPrice,int index)
        {
            int startIndex = index - TrendlineConfiguration.InnerTrendlinesSpan;
            int endIndex = index + TrendlineConfiguration.InnerTrendlinesSpan;

            int take = endIndex-startIndex;
            List<MarketData> rangePrices = prices.Skip(startIndex).Take(take).ToList();

            MarketData lowerPrice = rangePrices.Where(x => GetReferencePrice(x) < GetReferencePrice(currentPrice)).FirstOrDefault();

            if (lowerPrice==null && (rangePrices.Count == take))
            //if (!rangePrices.Any(x => GetLowestPrice(x) < GetLowestPrice(currentPrice)) && (rangePrices.Count == take))
            {
                //return true;
                return !ReapeatedValues(currentPrice, LocalMinimums,prices);//We avoid repeated minimum
            }
            else
            {
                return false;
            }
        }

        protected double GetReferencePrice(MarketData md)
        {
            double? price= ReferencePriceCalculator.GetReferencePrice(md, TrendlineConfiguration.CandleReferencePrice);
            if (price.HasValue)
                return price.Value;
            else
                throw new Exception($"Reference Price not found for symbol {md.Security.Symbol} at {md.MDEntryDate.Value}");
        }

        protected bool ReapeatedValues(MarketData currentPrice,List<MarketData> referencePrices, List<MarketData> histPrices)
        {
            
            foreach (MarketData refPrice in referencePrices)
            {
                if (GetReferencePrice(refPrice) == GetReferencePrice(currentPrice))
                {

                    if (currentPrice.MDEntryDate.HasValue && refPrice.MDEntryDate.HasValue)
                    {
                        int distBtwCandles = GetSpan(refPrice.GetReferenceDateTime().Value, currentPrice.GetReferenceDateTime().Value, histPrices);
                        if (distBtwCandles < _REPEATED_MAX_MIN_MAX_DISTANCE)
                            return true;
                    }
                }
            }

            return false;

        }

        protected bool EvalLocalMaximum(List<MarketData> prices, MarketData currentPrice, int index)
        {
            int startIndex = index - TrendlineConfiguration.InnerTrendlinesSpan;
            int endIndex = index + TrendlineConfiguration.InnerTrendlinesSpan;

            int take = endIndex - startIndex;
            List<MarketData> rangePrices = prices.Skip(startIndex).Take(take).ToList();

            MarketData higherPrice = rangePrices.Where(x => GetReferencePrice(x) > GetReferencePrice(currentPrice)).FirstOrDefault();

            if (higherPrice==null && (rangePrices.Count == take))
            //if (!rangePrices.Any(x => GetHighestPrice(x) > GetHighestPrice(currentPrice)) &&(rangePrices.Count == take))
            {
                //We avoid double trendlines because of equal highes/lowest
                //return true;
                return !ReapeatedValues(currentPrice, LocalMaximums,prices);//We avoid repeated maximums
            }
            else
                return false;
            
        }

        //Given new Local Max/Min--> Are they a trendline?
        //Compare all the inner prices
        protected bool EvalPotentialNewTrendlineBroken(List<MarketData> histPrices, Trendline possTrnd,bool downside)
        {
            //We just consider the prices outside of the local maximum/minimum range
            DateTime startDate = GetStartDate(possTrnd.StartDate,0);
            DateTime endDate = GetEndDate(possTrnd.EndDate,0);

            IList<MarketData> pricesInPeriod = histPrices
                                                .Where(x => DateTime.Compare(x.MDEntryDate.Value, startDate) >= 0
                                                && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToList();

            foreach (MarketData price in pricesInPeriod)
            {
                if (possTrnd.ValidateMinDistanceForBreakthrough(histPrices, price, _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE))
                {
                    if (possTrnd.EvalTrendlineJustBroken(price, histPrices, TrendlineConfiguration.PerforationThresholds, downside))
                        return true;
                }
            }

            return false;
        }

        protected void EvalSupport(Security stock,MarketData newLocalMinimum,
                                    List<MarketData> allHistoricalPrices, DateTime searchStart, DateTime searchEnd, bool markJustFound=false)
        {
            foreach (MarketData localMimimum in LocalMinimums)
            {
                if (EvalTimespan(newLocalMinimum,localMimimum))
                {
                    Trendline possibleTrendline = new Trendline()
                    {
                        JustBroken = false,
                        JustFound = markJustFound ,
                        Security = stock,
                        StartPrice = localMimimum,
                        EndPrice = newLocalMinimum,
                        BrokenDate = null,
                        TrendlineType = TrendlineType.Support,
                        CandleReferencePrice= TrendlineConfiguration.CandleReferencePrice,
                        Modified = true
                    };

                    OnLogMsg($"TrndlCreator-Found new potential support for symbo {stock.Symbol} for Date={newLocalMinimum.MDEntryDate.Value} and price={newLocalMinimum.Trade} (search from={searchStart} search to={searchEnd})", MessageType.Information);

                    if (!EvalPotentialNewTrendlineBroken(allHistoricalPrices, possibleTrendline, true))
                    {
                        if (!SupportTrendlines.Any(x => DateTime.Compare(x.StartDate, possibleTrendline.StartDate) == 0
                                                 && DateTime.Compare(x.EndDate, possibleTrendline.EndDate) == 0))
                        {
                            SupportTrendlines.Add(possibleTrendline);
                            OnLogMsg($"TrndlCreator-Trendline not broken or repeated --> ADDED", MessageType.Information);
                        }
                    }
                    else
                    {
                        OnLogMsg($"TrndlCreator-Trendline broken --> IGNORED", MessageType.Information);
                    }
                }
            }
        }

        protected void EvalResistance(Security stock, MarketData newLocalMaximum, 
                                     List<MarketData> allHistoricalPrices, DateTime searchStart, DateTime searchEnd, bool markJustFound=false
                                     )
        {
            foreach (MarketData localMaximum in LocalMaximums)
            {
                if (EvalTimespan(newLocalMaximum,localMaximum))
                {
                    Trendline possibleTrendline = new Trendline()
                    {
                        JustBroken = false,
                        JustFound = markJustFound,
                        Security = stock,
                        StartPrice = localMaximum,
                        EndPrice = newLocalMaximum,
                        BrokenDate = null,
                        TrendlineType = TrendlineType.Resistance,
                        CandleReferencePrice = TrendlineConfiguration.CandleReferencePrice,
                        Modified = true
                    };

                    OnLogMsg($"TrndlCreator-Found new potential resistance for Date={newLocalMaximum.MDEntryDate.Value} and price={newLocalMaximum.Trade} (search from={searchStart} search to={searchEnd})", MessageType.Information);

                    if (!EvalPotentialNewTrendlineBroken(allHistoricalPrices, possibleTrendline, false))
                    {
                        if (!ResistanceTrendlines.Any(x =>
                            DateTime.Compare(x.StartDate, possibleTrendline.StartDate) == 0
                            && DateTime.Compare(x.EndDate, possibleTrendline.EndDate) == 0))
                        {
                            ResistanceTrendlines.Add(possibleTrendline);
                            OnLogMsg($"TrndlCreator-Trendline not broken or repeated --> ADDED", MessageType.Information);
                        }
                    }
                    else {
                        OnLogMsg($"TrndlCreator-Trendline broken --> IGNORED", MessageType.Information);

                    }
                }
            }
        }

  
        //Given a new Price --> Does it break any trendline?
        public void EvalBrokenTrndLineForNewPrice(MarketData price,List<Trendline> trendlines, List<MarketData> histPrices, bool downside)
        {

            //Compared to BE we do not use
            //ValidDistanceToEndDate --> because this price could be an inner price
            //SoftSlope --> That is for opening. I could break a trendline and not open trade because of slope
            List<Trendline> activeTrendlines = trendlines.Where(x => !x.IsBroken(price.MDEntryDate)
                                                                && DateTime.Compare(x.EndDate, price.MDEntryDate.Value) < 0).ToList();

            foreach (Trendline trendline in activeTrendlines)
            {
                //if (GetSpan(trendline.EndDate, price.MDEntryDate.Value, histPrices) > 5)//Safety threshold
                //{

                    if (trendline.EvalTrendlineJustBroken(price, histPrices, TrendlineConfiguration.PerforationThresholds, downside))
                    {
                        trendline.DoBreak(price, histPrices);
                    }
                //}
            }
        }

        protected void ExtractPrevMinimums(bool usePrevTrendlines, DateTime? prevStartDate)
        {
            if (usePrevTrendlines)
            {
                LocalMinimums.Clear();
                
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

            if (usePrevTrendlines)
            {
                LocalMaximums.Clear();
                
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

        public List<Trendline> UpdatePotentialNewSupport(List<MarketData> allHistPrices, DateTime startDate, DateTime endDate,
                                                      MarketData lastCandle, bool markJustFound = false)
        {
            MarketData[] pricesArrToEval = allHistPrices.Where(x => DateTime.Compare(startDate, x.MDEntryDate.Value) <= 0
                                                               && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToArray();
            
            MarketData newPotLocalMin = pricesArrToEval.Where(x => DateTime.Compare(NextPotentialMinToEval, x.MDEntryDate.Value) == 0).FirstOrDefault();

            if (newPotLocalMin != null)
            {
                int index = pricesArrToEval.Where(x => DateTime.Compare(x.GetReferenceDateTime().Value, newPotLocalMin.GetReferenceDateTime().Value) <= 0).Count();

                //We check if the last candle breaks a trendline
                EvalBrokenTrndLineForNewPrice(lastCandle, SupportTrendlines, allHistPrices, true);

                if (EvalLocalMinimum(pricesArrToEval.ToList(), newPotLocalMin, index))
                {

                    EvalSupport(lastCandle.Security, newPotLocalMin, allHistPrices, startDate, endDate, markJustFound);

                    LocalMinimums.Add(newPotLocalMin);
                }
            }

            return SupportTrendlines;

        }

        public List<Trendline> ProcessSupportTrendlines(Security stock, List<MarketData> allHistoricalPrices,
                                                        bool processSafetyThreshold,DateTime startDate,DateTime endDate,
                                                        bool usePrevTrendlines,DateTime? startPrevTrendlineDate,
                                                        bool markJustFound=false)
        {
            int i = 0;
            ExtractPrevMinimums(usePrevTrendlines, startPrevTrendlineDate);

            MarketData[] pricesArr = allHistoricalPrices.Where(x => DateTime.Compare(startDate, x.MDEntryDate.Value) <= 0
                                                               && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToArray();

            foreach (MarketData price in pricesArr)
            {
                if (i < TrendlineConfiguration.InnerTrendlinesSpan && processSafetyThreshold)
                {
                    i++;
                    continue;
                }


                if (EvalLocalMinimum(pricesArr.ToList(), price, i))
                {

                    EvalSupport(stock, price, allHistoricalPrices,startDate,endDate,markJustFound);

                    LocalMinimums.Add(price);
                }

                EvalBrokenTrndLineForNewPrice(price, SupportTrendlines, allHistoricalPrices, true);

                i++;
            }

            return SupportTrendlines;

        }

        public List<Trendline> UpdatePotentialNewResistance(List<MarketData> allHistPrices, DateTime startDate, DateTime endDate,
                                                            MarketData lastCandle, bool markJustFound = false)
        {
            MarketData[] pricesArrToEval = allHistPrices.Where(x => DateTime.Compare(startDate, x.MDEntryDate.Value) <= 0
                                                               && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToArray();

            MarketData newPotLocalMax = pricesArrToEval.Where(x => DateTime.Compare(NextPotentialMaxToEval, x.MDEntryDate.Value) == 0).FirstOrDefault();

            if(newPotLocalMax!=null)
            {
                int index = pricesArrToEval.Where(x => DateTime.Compare(x.GetReferenceDateTime().Value, newPotLocalMax.GetReferenceDateTime().Value) <= 0).Count();

                //We check if the last candle breaks a trendline
                EvalBrokenTrndLineForNewPrice(lastCandle, ResistanceTrendlines, allHistPrices, false);

                if (EvalLocalMaximum(pricesArrToEval.ToList(), newPotLocalMax, index))
                {

                    EvalResistance(lastCandle.Security, newPotLocalMax, allHistPrices, startDate, endDate, markJustFound);

                    LocalMaximums.Add(newPotLocalMax);
                }
            }

            return ResistanceTrendlines;

        }

        public List<Trendline> ProcessResistanceTrendlines(Security security, List<MarketData> allHistPrices, 
                                                            bool processSafetyThreshold, DateTime startDate,
                                                            DateTime endDate,bool usePrevTrendlines,
                                                            DateTime? startPrevTrendlineDate,bool markJustFound=false)
        {
            int i = 0;
            ExtractPrevMaximums(usePrevTrendlines, startPrevTrendlineDate);

         MarketData[] pricesArr = allHistPrices.Where(x =>   DateTime.Compare(startDate, x.MDEntryDate.Value) <= 0 
                                                               && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToArray();


            foreach (MarketData price in pricesArr)
            {
                if (i < TrendlineConfiguration.InnerTrendlinesSpan && processSafetyThreshold)
                {
                    i++;
                    continue;
                }

                if (EvalLocalMaximum(pricesArr.ToList(), price, i))
                {

                    EvalResistance(security, price, allHistPrices, startDate, endDate, markJustFound);

                    LocalMaximums.Add(price);
                }

                EvalBrokenTrndLineForNewPrice(price, ResistanceTrendlines, allHistPrices, false);

                i++;
            }

            return ResistanceTrendlines;

        }

        public DateTime GetNextSlidingCandle(List<MarketData> prices, DateTime refDate)
        {
            if (prices == null || prices.Count == 0)
                throw new Exception($"Received empty array of candles calculating next sliding date!!!!");

            MarketData refMD = prices[0];

            MarketData[] priceArr = prices.OrderBy(x => x.GetReferenceDateTime().Value).ToArray();


            int refDateIndex = priceArr.Where(x => DateTime.Compare(x.GetReferenceDateTime().Value, refDate) <= 0).Count() - 1;
            if ((refDateIndex + 1) <= prices.Count)
            {//Jumps from 16:50  to 10:30 and so on

                if (prices[refDateIndex + 1].GetReferenceDateTime().HasValue)
                    return prices[refDateIndex + 1].GetReferenceDateTime().Value;
                else
                    throw new Exception($"Could not find a not full date for next day for date {refDate} for symbol {refMD.Security.Symbol}!!!");
            }
            else
                throw new Exception($"Could not find a candle for next date {refDate} for symbol {refMD.Security.Symbol}!!!");

        }


        public void MoveNextDateMinDateForResistances(List<MarketData> prices)
        {

            LastSafeMinDateResistances = GetNextSlidingCandle(prices, LastSafeMinDateResistances);
            NextPotentialMaxToEval = GetNextSlidingCandle(prices, NextPotentialMaxToEval);
        }

        public  void SetNextDateToStartForTrendlines(DateTime lastDate, List<MarketData> prices, int innerSpan)
        {

            List<MarketData> orderedPrices = prices.Where(x => DateTime.Compare(x.MDEntryDate.Value, lastDate) < 0).OrderByDescending(x => x.MDEntryDate.Value).ToList();
            List<MarketData> filteredPrices = orderedPrices
                                                            .Take(2 * innerSpan)
                                                            .OrderBy(x=>x.MDEntryDate.Value)
                                                            .ToList();
            MarketData firstMarketData = filteredPrices.FirstOrDefault();

            if (firstMarketData != null && firstMarketData.MDEntryDate.HasValue)
            {
                LastSafeMinDateSupports = firstMarketData.MDEntryDate.Value;
                LastSafeMinDateResistances = firstMarketData.MDEntryDate.Value;
                NextPotentialMinToEval = GetStartDate(firstMarketData.MDEntryDate.Value, innerSpan );
                NextPotentialMaxToEval = GetStartDate(firstMarketData.MDEntryDate.Value, innerSpan );
            }

        }

        public void MoveNextDateMinDateForSupports(List<MarketData> prices)
        {
            LastSafeMinDateSupports = GetNextSlidingCandle(prices, LastSafeMinDateSupports);
            NextPotentialMinToEval = GetNextSlidingCandle(prices, NextPotentialMinToEval);
        }

        public void ResetJustFound()
        {
            foreach (Trendline resistance in ResistanceTrendlines.Where(x=>x.JustFound))
            {
                resistance.JustFound = false;
            }
            
            foreach (Trendline support in SupportTrendlines.Where(x=>x.JustFound))
            {
                support.JustFound = false;
            }
            
        }
        
        #region Static Methods
        
        public static void EvalBrokenTrendlines(MonTrendlineTurtlesPosition monfPos,MarketData price)
        {
            if (TrdCreatorDict.ContainsKey(monfPos.Security.Symbol))
            {
                List<MarketData> histPrices = new List<MarketData>(monfPos.Candles.Values);
                histPrices = histPrices.OrderBy(x => x.MDEntryDate).ToList();
                
                TrdCreatorDict[monfPos.Security.Symbol].EvalBrokenTrndLineForNewPrice(price,monfPos.Supports,histPrices,true);
                TrdCreatorDict[monfPos.Security.Symbol].EvalBrokenTrndLineForNewPrice(price,monfPos.Resistances,histPrices,false);
            }
        }


        public static void InitializeCreator(Security sec,TrendlineConfiguration config, DateTime minSafeDate, OnLogMessage pOnLogMsg,
                                             int? _REPEATED_MAX_MIN_MAX_DISTANCE=null,int? _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE=null)
        {
            if(TrdCreatorDict==null)
                TrdCreatorDict=new Dictionary<string, TrendLineCreator>();


            TrendLineCreator trdCreator = new TrendLineCreator(sec,
                new TrendlineConfiguration()
                {
                    PerforationThresholds = config.PerforationThresholds,
                    InnerTrendlinesSpan = config.InnerTrendlinesSpan,
                    CandleReferencePrice=config.CandleReferencePrice
                }, CandleInterval.Minute_1, minSafeDate, minSafeDate, pOnLogMsg,
                _REPEATED_MAX_MIN_MAX_DISTANCE, _BREAKING_TRENDLINES_MIN_DISTANCE_TO_REF_DATE
                );

            if (!TrdCreatorDict.ContainsKey(sec.Symbol))
                TrdCreatorDict.Add(sec.Symbol, trdCreator);
            else
                TrdCreatorDict[sec.Symbol] = trdCreator;
        }

        public static List<Trendline> BuildResistances(Security sec, List<MarketData> prices,TrendlineConfiguration config)
        {

            if (prices != null && prices.Count > 0)
            {

                DateTime minDate = prices.OrderBy(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;
                DateTime maxDate = prices.OrderByDescending(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;

                List<Trendline> trendlines = TrdCreatorDict[sec.Symbol].ProcessResistanceTrendlines(sec,
                                            prices, true, minDate,
                                            maxDate, false, null);

                TrdCreatorDict[sec.Symbol].SetNextDateToStartForTrendlines(maxDate, prices, config.InnerTrendlinesSpan);
                return trendlines;
            }
            else
                return new List<Trendline>();

        }

        public static void MoveReferenceDateForResistances(List<MarketData> prices, string symbol)
        {

            TrdCreatorDict[symbol].MoveNextDateMinDateForResistances(prices);
        }

        public static List<Trendline> UpdateResistances(Security sec, List<MarketData> prices,MarketData lastCandle)
        {
            if (TrdCreatorDict == null)
                throw new Exception(
                    string.Format("TrendlineCreator has to be instantiated first building the resistances!"));
            
            DateTime minDate = TrdCreatorDict[sec.Symbol].LastSafeMinDateResistances;
            DateTime maxDate = lastCandle.MDEntryDate.Value;

            //List<Trendline> trendlines = TrdCreatorDict[sec.Symbol].ProcessResistanceTrendlines(sec,
            //    prices, true, minDate,
            //    maxDate, false, null,markJustFound:true);
            List<Trendline> trendlines = TrdCreatorDict[sec.Symbol].UpdatePotentialNewResistance(prices, minDate, maxDate, lastCandle, markJustFound: true);

            return trendlines.Where(x=>x.JustFound).ToList();

        }

        public static void ResetJustFound(Security sec)
        {
            TrdCreatorDict[sec.Symbol].ResetJustFound();
        }

        public static void MoveReferenceDateForSupports(List<MarketData> prices, string symbol)
        {

            TrdCreatorDict[symbol].MoveNextDateMinDateForSupports(prices);
        }


        public static List<Trendline> UpdateSupports(Security sec, List<MarketData> prices,MarketData lastCandle)
        {

            if (TrdCreatorDict == null)
                throw new Exception(
                    string.Format("TrendlineCreator has to be instantiated first building the supports!"));
            
            DateTime minDate = TrdCreatorDict[sec.Symbol].LastSafeMinDateSupports;
            DateTime maxDate = lastCandle.MDEntryDate.Value;

            List<Trendline> trendlines = TrdCreatorDict[sec.Symbol].UpdatePotentialNewSupport(prices, minDate, maxDate, lastCandle, markJustFound: true);
            

            //List<Trendline> trendlines = TrdCreatorDict[sec.Symbol].ProcessSupportTrendlines(sec,
            //                             prices, true, minDate,
            //                            maxDate, false, null,markJustFound:true);
            
            return trendlines.Where(x => x.JustFound).ToList();

        }
        
        public static List<Trendline> BuildSupports(Security sec, List<MarketData> prices,TrendlineConfiguration config)
        {
            if (prices != null && prices.Count > 0)
            {


                DateTime minDate = prices.OrderBy(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;
                DateTime maxDate = prices.OrderByDescending(x => x.MDEntryDate.Value).FirstOrDefault().MDEntryDate.Value;

                List<Trendline> trendlines = TrdCreatorDict[sec.Symbol].ProcessSupportTrendlines(sec,
                    prices, true, minDate, maxDate,
                    false, null);


                TrdCreatorDict[sec.Symbol].SetNextDateToStartForTrendlines(maxDate, prices, config.InnerTrendlinesSpan);
                return trendlines;
            }
            else
                return new List<Trendline>();

        }
        
        public static bool SymbolInitialized(string symbol)
        {
            return TrdCreatorDict.ContainsKey(symbol);
        }


        public static void AppendSupport(string symbol, Trendline trendline)
        {
            if(TrdCreatorDict.ContainsKey(symbol))
                TrdCreatorDict[symbol].SupportTrendlines.Add(trendline);
        }
        
        public static void AppendResistance(string symbol, Trendline trendline)
        {
            if(TrdCreatorDict.ContainsKey(symbol))
                TrdCreatorDict[symbol].ResistanceTrendlines.Add(trendline);
        }

        public static Trendline FetchResistance(string symbol, Trendline refTrendline)
        {
            Trendline memPosResistance = TrdCreatorDict[symbol].ResistanceTrendlines
                .FirstOrDefault(x => DateTime.Compare(x.StartDate, refTrendline.StartDate) == 0
                                     && DateTime.Compare(x.EndDate, refTrendline.EndDate) == 0
                                     && x.TrendlineType == refTrendline.TrendlineType);

            return memPosResistance;

        }
        
        public static Trendline FetchSupport(string symbol, Trendline refTrendline)
        {
            Trendline memPosSupport = TrdCreatorDict[symbol].SupportTrendlines
                .FirstOrDefault(x => DateTime.Compare(x.StartDate, refTrendline.StartDate) == 0
                                     && DateTime.Compare(x.EndDate, refTrendline.EndDate) == 0
                                     && x.TrendlineType == refTrendline.TrendlineType);

            return memPosSupport;

        }

        #endregion
        
        #endregion
    }
}
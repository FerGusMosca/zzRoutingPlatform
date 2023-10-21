using System;
using System.Collections.Generic;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;

namespace tph.TrendlineTurtles.BusinessEntities
{
    public enum TrendlineType
    {
        Support = 'S',
        Resistance = 'R'
    }

    public class Trendline
    {
        #region Constructors

        public Trendline()
        {
            Persisted = false;
        }

        #endregion
        
        #region Public Static Consts

        public static string _TRENDLINE_TYPE_RESISTANCE = "R";
        public static string _TRENDLINE_TYPE_SUPPORT = "S";
        
        #endregion
        
        #region Private static Consts

        public static double _SHORT_SOFT_UPWARD_SLOPE = 1 ;//1 degrees

        public static double _SHORT_SOFT_DOWNARD_SLOPE = 8;//8 degrees

        public static double _LONG_SOFT_UPWARD_SLOPE = 1 ;//1 degrees

        public static double _LONG_SOFT_DOWNARD_SLOPE = 8;//8 degrees
        
        #endregion
        
        #region Public Attributes
        
        public long Id { get; set; }

        public Security Security { get; set; }

        public MarketData StartPrice { get; set; }

        public MarketData EndPrice { get; set; }

        public DateTime? BrokenDate { get; set; }
        
        public double? BrokenTrendlinePrice { get; set; }

        public MarketData BrokenMarketPrice { get; set; }
        
        public TrendlineType TrendlineType { get; set; }
        
        public bool JustBroken { get; set; }
        
        public bool JustFound { get; set; }
        
        public bool Persisted { get; set; }
        
        public bool? ManualNew { get; set; }
        
        public bool? ToDisabled { get; set; }
        
        public bool? Disabled { get; set; }


        #region Indirect Attributes

        public string Symbol
        {

            get
            {
                if (Security != null)
                    return Security.Symbol;
                else
                    return null;
            }
            set
            {
                if (Security == null)
                    Security = new Security();

                Security.Symbol = value;
            }

        }

        public DateTime StartDate
        {

            get 
            {
                if (StartPrice != null && StartPrice.MDEntryDate.HasValue)
                    return StartPrice.MDEntryDate.Value;
                else
                    return DateTime.MinValue;
            }
            set 
            {
                if (StartPrice == null)
                    StartPrice = new MarketData();

                StartPrice.MDEntryDate = value;
            }
        
        }

        public DateTime EndDate
        {
            get
            {
                if (EndPrice != null && EndPrice.MDEntryDate.HasValue)
                    return EndPrice.MDEntryDate.Value;
                else
                    return DateTime.MaxValue;
            }
            set
            {
                if (EndPrice == null)
                    EndPrice = new MarketData();

                EndPrice.MDEntryDate= value;
            }
        
        }

        public double? StartPriceVal
        {
            get
            {
                if (StartPrice != null)
                    return GetPriceToUse(StartPrice);
                else
                    return 0;
            }
            set
            {
                StartPrice = new MarketData()
                {
                    MDEntryDate = StartDate, OpeningPrice = value,
                    TradingSessionHighPrice = value, TradingSessionLowPrice = value,
                    ClosingPrice = value, Trade = value
                };
            }
           
        }

        public double? EndPriceVal
        {
            get
            {
                if (EndPrice != null)
                    return GetPriceToUse(EndPrice);
                else
                    return 0;
            }
            set
            {
                EndPrice = new MarketData()
                {
                    MDEntryDate = StartDate, OpeningPrice = value,
                    TradingSessionHighPrice = value, TradingSessionLowPrice = value,
                    ClosingPrice = value, Trade = value
                };
            
            }
           
        }

        public bool Modified { get; set; }

        public List<MarketData> AllHistoricalPrices { get; set; }

        private double _Slope = double.MinValue;
        public double Slope
        {
            get 
            {
                if (_Slope == double.MinValue && AllHistoricalPrices!=null)
                {
                    int countDaysInTrendline = CountTradingUnits(AllHistoricalPrices, StartPrice.MDEntryDate.Value, EndPrice.MDEntryDate.Value);

                    _Slope = (GetPriceToUse(EndPrice).Value - GetPriceToUse(StartPrice).Value) / Convert.ToDouble(countDaysInTrendline);
                }

                return _Slope;
            }
        
        }

        public decimal FullSlope { get; set; }


        #endregion

        #endregion

        #region Privte Methods
        
        private int GetSpan(DateTime start, DateTime end,CandleInterval CandleInterval)
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

        private double? GetPriceToUse(MarketData price)
        {
            if (price == null)
                return null;

            if (TrendlineType == TrendlineType.Resistance)
                //return price.ClosingPrice > price.OpeningPrice ? price.ClosingPrice : price.OpeningPrice;
                return price.ClosingPrice;
            else if (TrendlineType == TrendlineType.Support)
                //return price.OpeningPrice < price.ClosingPrice ? price.OpeningPrice : price.ClosingPrice;
                return price.ClosingPrice;
            else
                return 0;
        }
        
        // Minutes, Hours, Days
        private int CountTradingUnits(List<MarketData> allHistoricalPrices,DateTime startDate,DateTime endDate)
        {
            int count = GetSpan(startDate, endDate, CandleInterval.Minute_1) ;

//            List<MarketData> tradingUnitsInPeriod = allHistoricalPrices.Where(x =>     DateTime.Compare(x.MDEntryDate.Value, startDate) >= 0
//                                                                                    && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToList();

            //count = tradingUnitsInPeriod.Count - 1;


            if (count < 0)
                throw new Exception(string.Format("Start date {0} older than historical prices end", startDate));

            return count;
        
        }

        #endregion

        #region Public Methods

        //Given a certain date, it estimates the trendprice
        public double CalculateTrendPrice(DateTime date, List<MarketData> allHistoricalPrices)
        {
            if(AllHistoricalPrices==null)
                AllHistoricalPrices = allHistoricalPrices;

            double finalPrice = 0;

            int countDaysFromStartPrice = CountTradingUnits(AllHistoricalPrices, StartPrice.MDEntryDate.Value, date);

            finalPrice = GetPriceToUse(StartPrice).Value + (Slope * Convert.ToDouble(countDaysFromStartPrice));

            return finalPrice;
        }
        
        public double GetSlopeDegrees()
        {
            double pctGrowth = (EndPrice.ClosingPrice.Value / StartPrice.ClosingPrice.Value) - 1;
            pctGrowth *= 100;
            
            int countUnitsInTrendline = CountTradingUnits(AllHistoricalPrices, StartPrice.MDEntryDate.Value, EndPrice.MDEntryDate.Value);

            double hourAdjust = 60 / Convert.ToDouble(countUnitsInTrendline);

            return pctGrowth * hourAdjust;
        }


        public bool IsSoftSlope(List<MarketData> allHistoricalPrices)
        {
            if (TrendlineType == TrendlineType.Resistance)
            {

                double degSlope = GetSlopeDegrees();

                if (degSlope > 0)
                    return degSlope < _LONG_SOFT_UPWARD_SLOPE;
                else
                {
                    //We avoid slopes that are too deep
                    return degSlope>(-1 * _LONG_SOFT_DOWNARD_SLOPE);
                }
            }
            else if (TrendlineType == TrendlineType.Support)
            {
                double degSlope = GetSlopeDegrees();
                
                if (degSlope > 0)
                    return degSlope < _SHORT_SOFT_UPWARD_SLOPE;
                else
                    return degSlope>(-1 * _SHORT_SOFT_DOWNARD_SLOPE);
            }
            else
                return true;
        
        }

        public string GetBrokenData()
        {
            if (BrokenDate.HasValue)
            {
                string resp = "";

                resp += string.Format(" Broken_Date={0} ",BrokenDate.Value);
                resp += string.Format(" Broken_Trendline_Price={0} ",BrokenTrendlinePrice);
                resp += string.Format(" Broken_Market_Price={0} ", BrokenMarketPrice != null ? BrokenMarketPrice.ClosingPrice.ToString() : "-");
                return resp;
            }
            else
            {
                return " no broken ";
            }
        }

        public bool IsBroken(DateTime? date)
        {
            if (!date.HasValue)
                return BrokenDate != null;
            
            
            if (BrokenDate != null)
            {
                //We validate that it was broken before than date

                if (DateTime.Compare(BrokenDate.Value, date.Value) <= 0)
                    return true;
                else
                    return false;
            }
            else
                return false;


        }

        public bool ValidDistanceToEndDate(DateTime date, int outerSpan,CandleInterval candleInterval)
        {
            int elapsed = GetSpan(EndDate, date, candleInterval);
            return elapsed >= outerSpan;
        }

        
        #endregion
    }
}
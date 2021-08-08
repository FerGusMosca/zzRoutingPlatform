using System;
using System.Collections.Generic;
using System.Linq;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;

namespace tph.BOBDayTurtles.BusinessEntities
{
    public enum TrendlineType
    {
        Support = 'S',
        Resistance = 'R'
    }
    
    public class Trendline
    {
        #region Private static Consts

        private static double _SOFT_UPWARD_SLOPE = 1 ;//1 degrees

        private static double _SOFT_DOWNARD_SLOPE = 8;//8 degrees

        #endregion
        #region Public Attributes

        public Security Security { get; set; }

        public MarketData StartPrice { get; set; }

        public MarketData EndPrice { get; set; }

        public DateTime? BrokenDate { get; set; }

        public TrendlineType TrendlineType { get; set; }


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
                    int countDaysInTrendline = CountTradingDays(AllHistoricalPrices, StartPrice.MDEntryDate.Value, EndPrice.MDEntryDate.Value);

                    _Slope = (GetPriceToUse(EndPrice).Value - GetPriceToUse(StartPrice).Value) / Convert.ToDouble(countDaysInTrendline);
                }

                return _Slope;
            }
        
        }

        public decimal FullSlope { get; set; }


        #endregion

        #endregion

        #region Privte Methods

        private double? GetPriceToUse(MarketData price)
        {
            if (price == null)
                return null;
            
            if (TrendlineType == TrendlineType.Resistance)
                //return price.TradingSessionHighPrice;
                return price.ClosingPrice;
            else if (TrendlineType == TrendlineType.Support)
                //return price.TradingSessionLowPrice;
                return price.OpeningPrice;
            else
                return 0;
            //  throw new Exception(string.Format("Invalid trendline type {0}", TrendlineType.ToString()));

        }

        private int CountTradingDays(List<MarketData> allHistoricalPrices,DateTime startDate,DateTime endDate)
        {
            int count = 0;

            List<MarketData> tradingDaysInPeriod = allHistoricalPrices.Where(x =>   DateTime.Compare(x.MDEntryDate.Value, startDate) >= 0
                                                                            && DateTime.Compare(x.MDEntryDate.Value, endDate) <= 0).ToList();

            count = tradingDaysInPeriod.Count;


            if (count == 0)
                throw new Exception(string.Format("Start date {0} older than historical prices end", startDate));

            //if (DateTime.Compare(allHistoricalPrices.OrderByDescending(x => x.Date).FirstOrDefault().Date, endDate.Date) < 0)
            //    throw new Exception(string.Format("End date {0} older than historical prices end", endDate));

            //if (DateTime.Compare(allHistoricalPrices.OrderBy(x => x.Date).FirstOrDefault().Date, startDate.Date) > 0)
            //    throw new Exception(string.Format("Start date {0} newer than historical prices first date", startDate));

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
           
            int countDaysFromStartPrice = CountTradingDays(AllHistoricalPrices, StartPrice.MDEntryDate.Value, date) - 1;

            finalPrice = GetPriceToUse(StartPrice).Value + (Slope * Convert.ToDouble(countDaysFromStartPrice));

            return finalPrice;
        }

        public bool IsSoftSlope(List<MarketData> allHistoricalPrices)
        {
            if (TrendlineType == TrendlineType.Resistance)
            {

                double radSlope = Math.Atan(Convert.ToDouble(Slope));

                double degSlope = (radSlope * 180) / Math.PI;

                if (degSlope > 0)
                    return degSlope < _SOFT_UPWARD_SLOPE;
                else
                {
                    //We avoid slopes that are too deep
                    return degSlope>(-1 * _SOFT_DOWNARD_SLOPE);
                }
            }
            else
                return true;
        
        }

        #endregion
    }
}
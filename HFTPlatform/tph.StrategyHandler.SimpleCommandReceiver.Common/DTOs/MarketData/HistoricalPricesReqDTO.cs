using System;
using Newtonsoft.Json;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class HistoricalPricesReqDTO:WebSocketMessage
    {

        #region Public Static Conts

        
        public static string _INT_1_MIN = "1 min";
        
        public static string _INT_5_MIN = "5 min";
        
        public static string _INT_1_HOUR = "1 hour";
        
        public static string _INT_5_HOUR = "5 hour";
        
        public static string _INT_DAY = "1 day";

        #endregion

        #region Constructors


        public HistoricalPricesReqDTO()
        {
            Msg = "HistoricalPricesRequest";
        }

        #endregion
        
        #region Public Attributes
        
        public int HistPrReqId { get; set; }

        public string Symbol { get; set; }
        
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy hh:mm:ss")]
        public DateTime? From { get; set; }
        
        
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy hh:mm:ss")]
        public  DateTime? To { get; set; }
        
        public CandleInterval CInterval { get; set; }
        
        public string Interval { get; set; }

        #endregion

        #region Public Methods

        public static string GetStrInterval(CandleInterval CInterval)
        {
            if (CInterval == CandleInterval.Minute_1)
                return _INT_1_MIN;
            else if (CInterval == CandleInterval.Minute_5)
                return _INT_5_MIN;
            else if (CInterval == CandleInterval.HOUR_1)
                return _INT_1_HOUR;
            else if (CInterval == CandleInterval.HOUR_5)
                return _INT_5_HOUR;
            else if (CInterval == CandleInterval.DAY)
                return _INT_DAY;
            else
                throw new Exception($"Interval not found {CInterval}");

        }
        
        public CandleInterval GetCandleInterval()
        {
            if (Interval == _INT_1_MIN)
                return CandleInterval.Minute_1;
            else if (Interval == _INT_5_MIN)
                return CandleInterval.Minute_5;
            else if (Interval == _INT_1_HOUR)
                return CandleInterval.HOUR_1;
            else if (Interval ==_INT_5_HOUR)
                return  CandleInterval.HOUR_5;
            else if (Interval == _INT_DAY)
                return CandleInterval.DAY;
            else
                throw new Exception($"Interval not found {Interval}");

        }

        #endregion
    }
}
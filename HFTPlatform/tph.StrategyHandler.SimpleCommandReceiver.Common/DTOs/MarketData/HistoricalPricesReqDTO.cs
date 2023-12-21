using System;
using Newtonsoft.Json;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class HistoricalPricesReqDTO:WebSocketMessage
    {

      

        #region Constructors


        public HistoricalPricesReqDTO()
        {
            Msg = "HistoricalPricesRequest";
        }

        #endregion
        
        #region Public Attributes
        
        public int HistPrReqId { get; set; }

        public string Symbol { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public SecurityType? SecurityType { get; set; }
        
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

            return CandleIntervalTranslator.GetStrInterval(CInterval);

        }
        
        public CandleInterval GetCandleInterval()
        {
            return CandleIntervalTranslator.GetCandleInterval(Interval);

        }

        #endregion
    }
}
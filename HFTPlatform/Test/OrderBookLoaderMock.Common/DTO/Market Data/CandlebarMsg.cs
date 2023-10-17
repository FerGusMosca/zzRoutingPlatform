using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using zHFT.Main.BusinessEntities.Securities;

namespace OrderBookLoaderMock.Common.DTO
{
    public class CustomDateTimeConverter : IsoDateTimeConverter
    {
        public CustomDateTimeConverter()
        {
            base.DateTimeFormat = "dd-MM-yyyy HH:mm:ss";
        }
    }
    
    public class CandlebarMsg: WebSocketMessage
    {
        #region Public Attributes
        
        public Security Security { get; set; }
        
        
        public double? High { get; set; }
        
        public double? Open { get; set; }
        
        public double? Close { get; set; }
        
        public double? Low { get; set; }
        
        public double? Trade { get; set; }
        
        public int Volume { get; set; }
        
        public string Symbol { get; set; }
        
        public string Key { get; set; }
        
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime Date { get; set; }
        
        #endregion
    }
}
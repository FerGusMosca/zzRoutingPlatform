using System;
using System.ComponentModel;
using Newtonsoft.Json;
using tph.StrategyHandler.SimpleCommandReceiver.Common.Converters;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class CandlebarDTO:Candlebar
    {
         #region Constructors

        public CandlebarDTO(Candlebar cb)
        {
            Open = cb.Open;
            Close = cb.Close;
            High = cb.High;
            Low = cb.Low;
            Security = cb.Security;
            Trade = cb.Trade;
            Volume = cb.Volume;
            Key = cb.Key;
            Date = cb.Date;
            Symbol = cb.Security.Symbol;

        }

        #endregion
        
        #region Public Attributes 
        
        public string Msg = "CandlebarMsg";
        
        public string Symbol { get; set; }
        
        public string Key { get; set; }
        
        [JsonConverter(typeof(WebsocketConnectorDateTimeConverter))]
        public override DateTime Date { get; set; }
        
        #endregion
    }
}
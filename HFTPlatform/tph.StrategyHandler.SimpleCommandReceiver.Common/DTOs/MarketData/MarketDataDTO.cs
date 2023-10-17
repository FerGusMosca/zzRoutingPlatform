using System;
using Newtonsoft.Json;
using tph.StrategyHandler.SimpleCommandReceiver.Common.Converters;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class MarketDataDTO:zHFT.Main.BusinessEntities.Market_Data.MarketData
    {
        #region Constructors


        public MarketDataDTO()
        {
            
        }

        public MarketDataDTO(zHFT.Main.BusinessEntities.Market_Data.MarketData md)
        {
            OpeningPrice = md.OpeningPrice;
            ClosingPrice = md.ClosingPrice;
            SettlementPrice = md.SettlementPrice;
            TradingSessionHighPrice = md.TradingSessionHighPrice;
            TradingSessionLowPrice = md.TradingSessionLowPrice;
            TradingSessionVWAPPrice = md.TradingSessionVWAPPrice;
            Imbalance = md.Imbalance;
            TradeVolume = md.TradeVolume;
            OpenInterest = md.OpenInterest;
            CompositeUnderlyingPrice = md.CompositeUnderlyingPrice;
            MarginRate = md.MarginRate;
            MidPrice = md.MidPrice;
            SettleHighPrice = md.SettleHighPrice;
            SettlPriorPrice = md.SettlPriorPrice;
            SessionHighBid = md.SessionHighBid;
            SessionLowOffer = md.SessionLowOffer;
            EarlyPrices = md.EarlyPrices;
            AuctionClearingPrice = md.AuctionClearingPrice;
            Trade = md.Trade;
            BestBidPrice = md.BestBidPrice;
            BestBidSize = md.BestBidSize;
            BestBidExch = md.BestBidExch;
            BestAskPrice = md.BestAskPrice;
            BestAskSize = md.BestAskSize;
            BestAskExch = md.BestAskExch;

            BestBidCashSize = md.BestBidCashSize;
            BestAskCashSize = md.BestAskCashSize;

            CashVolume = md.CashVolume;
            NominalVolume = md.NominalVolume;
            MDUpdateAction = md.MDUpdateAction;
            Currency = md.Currency;
            MDEntryDate = md.MDEntryDate;
            MDLocalEntryDate = md.MDLocalEntryDate;
            TickDirection = md.TickDirection;
            MDTradeSize = md.MDTradeSize;
            SettlType = md.SettlType;
            SettlDate = md.SettlDate;
            LastTradeDateTime = md.LastTradeDateTime;
            Security = md.Security;
            Symbol = md.Security != null ? md.Security.Symbol : null;

        }

        #endregion
        
        #region Public Attributes 
        
        public string Msg = "MarketDataMsg";
        
        public string Symbol { get; set; }
        
       [JsonConverter(typeof(WebsocketConnectorFullTimeConverter))]
        public DateTime? MDEntryDate { get; set; }
        
        [JsonConverter(typeof(WebsocketConnectorFullTimeConverter))]
        public DateTime? MDLocalEntryDate { get; set; }
        
        [JsonConverter(typeof(WebsocketConnectorFullTimeConverter))]
        public DateTime? SettlDate { get; set; }

        [JsonConverter(typeof(WebsocketConnectorFullTimeConverter))]
        public DateTime? LastTradeDateTime { get; set; }
        
        
        #endregion
        
        
        public override string ToString()
        {
            return string.Format(
                "DateTime={9} Symbol={0} Open={1} High={2} Low={3} Close={4} Trade={5} BestBidPx={6} BestAskPx={7} Volume={8}",
                Security.Symbol, OpeningPrice, TradingSessionHighPrice, TradingSessionLowPrice,
                ClosingPrice, Trade, BestBidPrice, BestAskPrice, TradeVolume,
                MDLocalEntryDate);
        }
    }
}
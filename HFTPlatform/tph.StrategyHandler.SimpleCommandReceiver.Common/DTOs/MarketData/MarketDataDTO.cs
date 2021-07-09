namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData
{
    public class MarketDataDTO:zHFT.Main.BusinessEntities.Market_Data.MarketData
    {
        #region Constructors

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
        
        #endregion
    }
}
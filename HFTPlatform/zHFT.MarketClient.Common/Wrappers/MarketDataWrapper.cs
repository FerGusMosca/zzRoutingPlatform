using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Common.Wrappers
{
    public class MarketDataWrapper : Wrapper
    {
        #region Protected Attributes

        protected Security Security { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion

        #region Constructors

        public MarketDataWrapper(Security pSecurity, IConfiguration pConfig) 
        {
            Security = pSecurity;

            Config = pConfig;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Security != null)
            {
                string resp = string.Format("Symbol={0} Exchange={1} SecType={2} Currency={3}",
                                           Security.Symbol, Security.Exchange, Security.SecType.ToString(), Security.Currency);


                if (Security.MarketData != null)
                {

                    resp += "-";
                    resp += string.Format(" Open={0}", Security.MarketData.OpeningPrice.HasValue ? Security.MarketData.OpeningPrice.Value.ToString("#.##") : "no data");
                    resp += string.Format(" High={0}", Security.MarketData.TradingSessionHighPrice.HasValue ? Security.MarketData.TradingSessionHighPrice.Value.ToString("#.##") : "no data");
                    resp += string.Format(" Low={0}", Security.MarketData.TradingSessionHighPrice.HasValue ? Security.MarketData.TradingSessionLowPrice.Value.ToString("#.##") : "no data");
                    resp += string.Format(" Close={0}", Security.MarketData.ClosingPrice.HasValue ? Security.MarketData.ClosingPrice.Value.ToString("#.##") : "no data");
                    resp += string.Format(" Volume={0}", Security.MarketData.TradeVolume.HasValue ? Security.MarketData.TradeVolume.Value.ToString("#.##") : "no data");
                    resp += string.Format(" LastPrice={0}", Security.MarketData.Trade.HasValue ? Security.MarketData.Trade.Value.ToString("#.##") : "no data");
                    resp += string.Format(" LastTradeSize={0}", Security.MarketData.MDTradeSize.HasValue ? Security.MarketData.MDTradeSize.Value.ToString("#.##") : "no data");
                    // += string.Format(" EntryDate={0}", Security.MarketData.MDLocalEntryDate.HasValue ? Security.MarketData.MDLocalEntryDate.Value.ToString("dd/MM/yyyy HH:mm:ss") : "no data");

                    resp += string.Format(" BestBidPrice={0}", Security.MarketData.BestBidPrice.HasValue ? Security.MarketData.BestBidPrice.Value.ToString("#.##") : "no data");
                    resp += string.Format(" BestBidSize={0}", Security.MarketData.BestBidSize.HasValue ? Security.MarketData.BestBidSize.Value.ToString() : "no data");
                    resp += string.Format(" BestBidExch={0}", Security.MarketData.BestBidExch!=null ? Security.MarketData.BestBidExch : "no data");
                    resp += string.Format(" BestAskPrice={0}", Security.MarketData.BestAskPrice.HasValue ? Security.MarketData.BestAskPrice.Value.ToString("#.##") : "no data");
                    resp += string.Format(" BestAskSize={0}", Security.MarketData.BestAskSize.HasValue ? Security.MarketData.BestAskSize.Value.ToString() : "no data");
                    resp += string.Format(" BestAskExch={0}", Security.MarketData.BestAskExch != null ? Security.MarketData.BestAskExch : "no data");
                    resp += string.Format(" CompositeUnderlyingPrice={0}", Security.MarketData.CompositeUnderlyingPrice != null ? Security.MarketData.CompositeUnderlyingPrice.Value.ToString() : "no data");

                }
                return resp;
            }
            else
                return "";
        }


        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

            if (Security == null)
                return MarketDataFields.NULL;

            if (mdField == MarketDataFields.Symbol)
                return Security.Symbol;
            else if (mdField == MarketDataFields.SecurityType)
                return Security.SecType;
            else if (mdField == MarketDataFields.Currency)
                return Security.Currency;
            else if (mdField == MarketDataFields.MDMkt)
                return Security.Exchange;

            if (Security == null || Security.MarketData == null)
                return MarketDataFields.NULL;

            if (mdField == MarketDataFields.OpeningPrice)
                return Security.MarketData.OpeningPrice;
            else if (mdField == MarketDataFields.ClosingPrice)
                return Security.MarketData.ClosingPrice;
            else if (mdField == MarketDataFields.TradingSessionHighPrice)
                return Security.MarketData.TradingSessionHighPrice;
            else if (mdField == MarketDataFields.TradingSessionLowPrice)
                return Security.MarketData.TradingSessionLowPrice;
            else if (mdField == MarketDataFields.TradingSessionLowPrice)
                return Security.MarketData.TradingSessionLowPrice;
            else if (mdField == MarketDataFields.TradeVolume)
                return Security.MarketData.TradeVolume;
            else if (mdField == MarketDataFields.OpenInterest)
                return Security.MarketData.OpenInterest;
            else if (mdField == MarketDataFields.SettlType)
                return Security.MarketData.SettlType;
            else if (mdField == MarketDataFields.CompositeUnderlyingPrice)
                return Security.MarketData.CompositeUnderlyingPrice;
            else if (mdField == MarketDataFields.MidPrice)
                return Security.MarketData.MidPrice;
            else if (mdField == MarketDataFields.SessionHighBid)
                return Security.MarketData.SessionHighBid;
            else if (mdField == MarketDataFields.SessionLowOffer)
                return Security.MarketData.SessionLowOffer;
            else if (mdField == MarketDataFields.EarlyPrices)
                return Security.MarketData.EarlyPrices;
            else if (mdField == MarketDataFields.Trade)
                return Security.MarketData.Trade;
            else if (mdField == MarketDataFields.MDTradeSize)
                return Security.MarketData.MDTradeSize;
            else if (mdField == MarketDataFields.BestBidPrice)
                return Security.MarketData.BestBidPrice;
            else if (mdField == MarketDataFields.BestAskPrice)
                return Security.MarketData.BestAskPrice;
            else if (mdField == MarketDataFields.BestBidSize)
                return Security.MarketData.BestBidSize;
            else if (mdField == MarketDataFields.BestBidCashSize)
                return Security.MarketData.BestBidCashSize;
            else if (mdField == MarketDataFields.BestAskSize)
                return Security.MarketData.BestAskSize;
            else if (mdField == MarketDataFields.BestAskCashSize)
                return Security.MarketData.BestAskCashSize;
            else if (mdField == MarketDataFields.BestBidExch)
                return Security.MarketData.BestBidExch;
            else if (mdField == MarketDataFields.BestAskExch)
                return Security.MarketData.BestAskExch;
            else if (mdField == MarketDataFields.MDEntryDate)
                return Security.MarketData.MDEntryDate;
            else if (mdField == MarketDataFields.MDEntryTime)
                return Security.MarketData.MDLocalEntryDate;
            else if (mdField == MarketDataFields.MDLocalEntryDate)
                return Security.MarketData.MDLocalEntryDate;
            else if (mdField == MarketDataFields.ReverseMarketData)
                return Security.ReverseMarketData;
            else if (mdField == MarketDataFields.LastTradeDateTime)
                return Security.MarketData.LastTradeDateTime;

            return MarketDataFields.NULL;
        }

        public override Main.Common.Enums.Actions GetAction()
        {
            return Actions.MARKET_DATA;
        }

        #endregion
    }
}

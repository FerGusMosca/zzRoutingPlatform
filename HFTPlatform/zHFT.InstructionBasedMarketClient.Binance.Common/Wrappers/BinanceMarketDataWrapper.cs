using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Models.Market;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.MarketClient.Common.Wrappers;

namespace zHFT.InstructionBasedMarketClient.Binance.Common.Wrappers
{
    public class BinanceMarketDataWrapper : MarketDataWrapper
    {
        #region Constructors

        public BinanceMarketDataWrapper(Security pSecurity, IConfiguration pConfig) 
                :base(pSecurity,pConfig)
        {
            IsOrderBook = false;
        }
        
        public BinanceMarketDataWrapper(Security pSecurity,List<OrderBookOffer> pBids,List<OrderBookOffer> pAsks, IConfiguration pConfig) 
            :base(pSecurity,pConfig)
        {
            Bids = pBids;
            Asks = pAsks;
            IsOrderBook = true;
        }

        #endregion
        
        #region Protected Attributes

        protected bool IsOrderBook { get; set; }
        
        protected List<OrderBookOffer>  Bids { get; set; }
        
        protected List<OrderBookOffer>  Asks { get; set; }
        
        
        #endregion
        
        #region Protected Methods

        public decimal? GetBidPxi(int i)
        {
            if (Bids != null && Bids.Count > i)
                return Bids[i].Price;
            else
                return null;
        }
        
        public decimal? GetBidSizei(int i)
        {
            if (Bids != null && Bids.Count > i)
                return Bids[i].Quantity;
            else
                return null;
        }
        
        public decimal? GetAskPxi(int i)
        {
            if (Asks != null && Asks.Count > i)
                return Asks[i].Price;
            else
                return null;
        }
        
        public decimal? GetAskSizei(int i)
        {
            if (Asks != null && Asks.Count > i)
                return Asks[i].Quantity;
            else
                return null;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (Security != null)
            {
                string resp = string.Format("Symbol={0} ", Security.Symbol);

                if (Security.MarketData != null)
                {
                    resp += string.Format(" LastPrice={0}", Security.MarketData.Trade.HasValue ? Security.MarketData.Trade.Value.ToString("0.########") : "no data");
                    resp += string.Format(" BestBidPrice={0}", Security.MarketData.BestBidPrice.HasValue ? Security.MarketData.BestBidPrice.Value.ToString("0.########") : "no data");
                    resp += string.Format(" BestAskPrice={0}", Security.MarketData.BestAskPrice.HasValue ? Security.MarketData.BestAskPrice.Value.ToString("0.########") : "no data");

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
            else if (mdField == MarketDataFields.BidEntryPx0)
                return GetBidPxi(0);
            else if (mdField == MarketDataFields.BidEntryPx1)
                return GetBidPxi(1);
            else if (mdField == MarketDataFields.BidEntryPx2)
                return GetBidPxi(2);
            else if (mdField == MarketDataFields.BidEntryPx3)
                return GetBidPxi(3);
            else if (mdField == MarketDataFields.BidEntryPx4)
                return GetBidPxi(4);
            else if (mdField == MarketDataFields.AskEntryPx0)
                return GetAskPxi(0);
            else if (mdField == MarketDataFields.AskEntryPx1)
                return GetAskPxi(1);
            else if (mdField == MarketDataFields.AskEntryPx2)
                return GetAskPxi(2);
            else if (mdField == MarketDataFields.AskEntryPx3)
                return GetAskPxi(3);
            else if (mdField == MarketDataFields.AskEntryPx4)
                return GetAskPxi(4);
            else if (mdField == MarketDataFields.BidEntrySize0)
                return GetBidSizei(0);
            else if (mdField == MarketDataFields.BidEntrySize1)
                return GetBidSizei(1);
            else if (mdField == MarketDataFields.BidEntrySize2)
                return GetBidSizei(2);
            else if (mdField == MarketDataFields.BidEntrySize3)
                return GetBidSizei(3);
            else if (mdField == MarketDataFields.BidEntrySize4)
                return GetBidSizei(4);
            else if (mdField == MarketDataFields.AskEntrySize0)
                return GetAskSizei(0);
            else if (mdField == MarketDataFields.AskEntrySize1)
                return GetAskSizei(1);
            else if (mdField == MarketDataFields.AskEntrySize2)
                return GetAskSizei(2);
            else if (mdField == MarketDataFields.AskEntrySize3)
                return GetAskSizei(3);
            else if (mdField == MarketDataFields.AskEntrySize4)
                return GetAskSizei(4);
            else
                return base.GetField(field);
        }
        
        public override Main.Common.Enums.Actions GetAction()
        {
            return IsOrderBook ? Actions.ORDER_BOOK : Actions.MARKET_DATA;
        }

        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;

namespace zHFT.MarketClient.Quickfix.Common.Wrappers
{
    public class OrderBookWrapper : Wrapper
    {
        #region Private Attributes

        protected QuickFix50.MarketDataIncrementalRefresh IncrementalRefresh { get; set; }

        protected QuickFix50.MarketDataSnapshotFullRefresh FullRefresh { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion

        #region Constructors

        public OrderBookWrapper(QuickFix50.MarketDataIncrementalRefresh incRefresh, IConfiguration pConfig) 
        {
            IncrementalRefresh = incRefresh;
            if (pConfig is Configuration.Configuration)
                Config = (Configuration.Configuration)pConfig;
        }

        public OrderBookWrapper(QuickFix50.MarketDataSnapshotFullRefresh fullRefresh, IConfiguration pConfig)
        {
            FullRefresh = fullRefresh;
            if (pConfig is Configuration.Configuration)
                Config = (Configuration.Configuration)pConfig;
        }

        #endregion

        #region Private Methods

        protected object GetIncrementalRefresh(Main.Common.Enums.Fields field)
        {

            OrderBookFields obField = (OrderBookFields)field;

            if (obField == OrderBookFields.Symbol)
                return OrderBookFields.NULL;//Extraer el Ticker del objeto FIX
            else if (obField == OrderBookFields.Bids)
                return MarketDataFields.NULL;//Extraer los bids del objeto FIX
            else if (obField == OrderBookFields.Asks)
                return MarketDataFields.NULL;//Extraer los asks del objeto FIX
            else
                return MarketDataFields.NULL;

        }


        protected object GetFullRefresh(Main.Common.Enums.Fields field)
        {
            OrderBookFields obField = (OrderBookFields)field;

            if (obField == OrderBookFields.Symbol)
                return OrderBookFields.NULL;//Extraer el Ticker del objeto FIX
            else if (obField == OrderBookFields.Bids)
                return MarketDataFields.NULL;//Extraer los bids del objeto FIX
            else if (obField == OrderBookFields.Asks)
                return MarketDataFields.NULL;//Extraer los asks del objeto FIX
            else
                return MarketDataFields.NULL;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return "";
        }

        public override object GetField(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

            if (IncrementalRefresh != null)
                return GetIncrementalRefresh(field);
            else if (FullRefresh != null)
                return GetFullRefresh(field);
            else
                return Fields.NULL;
        }

        public override Actions GetAction()
        {
            return Actions.ORDER_BOOK;
        }

        #endregion
    }
}

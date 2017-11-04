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
    public class MarketDataWrapper:Wrapper
    {
        #region Private Attributes

        protected QuickFix50.MarketDataIncrementalRefresh IncrementalRefresh { get; set; }

        protected QuickFix50.MarketDataSnapshotFullRefresh FullRefresh { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion


        #region Constructors

        public MarketDataWrapper(QuickFix50.MarketDataIncrementalRefresh incRefresh, IConfiguration pConfig) 
        {
            IncrementalRefresh = incRefresh;
            if (pConfig is Configuration.Configuration)
                Config = (Configuration.Configuration)pConfig;
        }

        public MarketDataWrapper(QuickFix50.MarketDataSnapshotFullRefresh fullRefresh, IConfiguration pConfig)
        {
            FullRefresh = fullRefresh;
            if (pConfig is Configuration.Configuration)
                Config = (Configuration.Configuration)pConfig;
        }

        #endregion

        #region Private Methods

        protected object GetIncrementalRefresh(Main.Common.Enums.Fields field)
        {

            MarketDataFields mdField = (MarketDataFields)field;

            if (field == MarketDataFields.Symbol)
                return MarketDataFields.NULL;//Extraer el Ticker del objeto FIX
            else if (field == MarketDataFields.Trade)
                return MarketDataFields.NULL;//Extraer el ultimo precio negociado del objeto FIX
            else if (field == MarketDataFields.TradeVolume)
                return MarketDataFields.NULL;//Extraer el volumen negociado del objeto FIX
            else if (field == MarketDataFields.TradingSessionHighPrice)
                return MarketDataFields.NULL;//Extraer el TradingSessionHighPrice negociado del objeto FIX
            else if (field == MarketDataFields.TradingSessionLowPrice)
                return MarketDataFields.NULL;//Extraer el TradingSessionHighPrice negociado del objeto FIX
           
            else
                return MarketDataFields.NULL;
        
        }


        protected object GetFullRefresh(Main.Common.Enums.Fields field)
        {
            MarketDataFields mdField = (MarketDataFields)field;

            if (field == MarketDataFields.Symbol)
                return MarketDataFields.NULL;//Extraer el Ticker del objeto FIX
            else if (field == MarketDataFields.Trade)
                return MarketDataFields.NULL;//Extraer el ultimo precio negociado del objeto FIX
            else if (field == MarketDataFields.TradeVolume)
                return MarketDataFields.NULL;//Extraer el volumen negociado del objeto FIX
            else if (field == MarketDataFields.TradingSessionHighPrice)
                return MarketDataFields.NULL;//Extraer el TradingSessionHighPrice negociado del objeto FIX
            else if (field == MarketDataFields.TradingSessionLowPrice)
                return MarketDataFields.NULL;//Extraer el TradingSessionHighPrice negociado del objeto FIX

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
            MarketDataFields ordField = (MarketDataFields)field;

            if (IncrementalRefresh != null)
                return GetIncrementalRefresh(field);
            else if (FullRefresh != null)
                return GetFullRefresh(field);
            else
                return Fields.NULL;


          
        }

        public override Actions GetAction()
        {
            return Actions.MARKET_DATA;
        }

        #endregion
    }
}

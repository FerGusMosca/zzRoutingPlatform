using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.StrategyHandler.SecurityListSaver.BusinessEntities;
using zHFT.StrategyHandler.SecurityListSaver.DataAccess;

namespace zHFT.StrategyHandler.SecurityListSaver.DataAccessLayer.Managers
{
    public class StockMarkeDataManager : MappingEnabledAbstract
    {
        #region Constructors

        public StockMarkeDataManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(Security security,stocks_market_data stockMarketDataDB)
        {

            stockMarketDataDB.symbol = security.Symbol;
            stockMarketDataDB.date = security.MarketData.MDEntryDate.Value.Date;
            stockMarketDataDB.apertura = security.MarketData.OpeningPrice;
            stockMarketDataDB.maximo = security.MarketData.TradingSessionHighPrice;
            stockMarketDataDB.minimo = security.MarketData.TradingSessionLowPrice;
            stockMarketDataDB.cierre = security.MarketData.ClosingPrice;
            stockMarketDataDB.volumen_nominal = security.MarketData.NominalVolume;
            stockMarketDataDB.volumen_monetario = security.MarketData.CashVolume;
            stockMarketDataDB.precio_ultima_operacion = security.MarketData.Trade;
            stockMarketDataDB.volumen_ultima_operacion = security.MarketData.TradeVolume;
            stockMarketDataDB.mejor_bid_precio = security.MarketData.BestBidPrice;
            stockMarketDataDB.mejor_bid_tamano = security.MarketData.BestBidSize;
            stockMarketDataDB.mejor_bid_exch = security.MarketData.BestBidExch;
            stockMarketDataDB.mejor_ask_precio = security.MarketData.BestAskPrice;
            stockMarketDataDB.mejor_ask_tamano = security.MarketData.BestAskSize;
            stockMarketDataDB.mejor_ask_exch = security.MarketData.BestAskExch;
            stockMarketDataDB.moneda = security.MarketData.Currency;
            stockMarketDataDB.sesion = security.MarketData.SettlType.ToString();
            stockMarketDataDB.timestamp = security.MarketData.MDEntryDate.Value;
     
        }


        private stocks_market_data Map(Security security)
        {
            stocks_market_data stockMarketDataDB = new stocks_market_data();
            FieldMap(security, stockMarketDataDB);
            return stockMarketDataDB;
        }

        #endregion

        #region Public Methods

        public void Persist(Security security)
        {
            if (security.MarketData == null || security.MarketData.MDEntryDate == null)
                throw new Exception(string.Format("Could not find market data for symbol {0}", security.Symbol));

            //Insert
            stocks_market_data currentMD = ctx.stocks_market_data.Where(x => x.symbol == security.Symbol 
                                                                            && x.date.Day==security.MarketData.MDEntryDate.Value.Day
                                                                            && x.date.Month == security.MarketData.MDEntryDate.Value.Month
                                                                            && x.date.Year==security.MarketData.MDEntryDate.Value.Year).FirstOrDefault();
                                                                            
            if (currentMD == null)
            {
                stocks_market_data marketDataDB = Map(security);
                ctx.stocks_market_data.AddObject(marketDataDB);
                ctx.SaveChanges();
            }
            else
            {
                FieldMap(security, currentMD);
                ctx.SaveChanges();
            }
        }

        #endregion
    }
}

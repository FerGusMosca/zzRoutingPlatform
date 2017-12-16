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
    public class OptionMarketDataManager : MappingEnabledAbstract
    {
         #region Constructors

        public OptionMarketDataManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(Option option, options_market_data optionkMarketDataDB)
        {
            optionkMarketDataDB.option_id = option.Id;
            optionkMarketDataDB.symbol = option.Symbol;
            optionkMarketDataDB.symbol_sfx = option.SymbolSfx;
            optionkMarketDataDB.date = option.MarketData.MDEntryDate.Value.Date;
            optionkMarketDataDB.apertura = option.MarketData.OpeningPrice;
            optionkMarketDataDB.maximo = option.MarketData.TradingSessionHighPrice;
            optionkMarketDataDB.minimo = option.MarketData.TradingSessionLowPrice;
            optionkMarketDataDB.cierre = option.MarketData.ClosingPrice;
            optionkMarketDataDB.volumen_nominal = option.MarketData.NominalVolume;
            optionkMarketDataDB.volumen_monetario = option.MarketData.CashVolume;
            optionkMarketDataDB.precio_ultima_operacion = option.MarketData.Trade;
            optionkMarketDataDB.volumen_ultima_operacion = option.MarketData.MDTradeSize;
            optionkMarketDataDB.mejor_bid_precio = option.MarketData.BestBidPrice;
            optionkMarketDataDB.mejor_bid_tamano = option.MarketData.BestBidSize;
            optionkMarketDataDB.mejor_bid_exch = option.MarketData.BestBidExch;
            optionkMarketDataDB.mejor_ask_precio = option.MarketData.BestAskPrice;
            optionkMarketDataDB.mejor_ask_tamano = option.MarketData.BestAskSize;
            optionkMarketDataDB.mejor_ask_exch = option.MarketData.BestAskExch;
            optionkMarketDataDB.moneda = option.MarketData.Currency;
            optionkMarketDataDB.sesion = option.MarketData.SettlType.ToString();
            optionkMarketDataDB.timestamp = option.MarketData.MDEntryDate.Value;

        }


        private options_market_data Map(Option option)
        {
            options_market_data optionkMarketDataDB = new options_market_data();
            FieldMap(option, optionkMarketDataDB);
            return optionkMarketDataDB;
        }

        #endregion

        #region Public Methods

        public void Persist(Option option)
        {
            if (option.MarketData == null || option.MarketData.MDEntryDate == null)
                throw new Exception(string.Format("Could not find market data for symbol {0}", option.Symbol));

            //Insert
            options_market_data currentMD = ctx.options_market_data.Where(x => x.option_id == option.Id
                                                                            && x.date.Day == option.MarketData.MDEntryDate.Value.Day
                                                                            && x.date.Month == option.MarketData.MDEntryDate.Value.Month
                                                                            && x.date.Year == option.MarketData.MDEntryDate.Value.Year).FirstOrDefault();

            if (currentMD == null)
            {
                options_market_data marketDataDB = Map(option);
                ctx.options_market_data.AddObject(marketDataDB);
                ctx.SaveChanges();
            }
            else
            {
                FieldMap(option, currentMD);
                ctx.SaveChanges();
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.SecurityListSaver.BusinessEntities;
using zHFT.StrategyHandler.SecurityListSaver.DataAccess;

namespace zHFT.StrategyHandler.SecurityListSaver.DataAccessLayer.Managers
{
    public class StockManager : MappingEnabledAbstract
    {
        #region Constructors

        public StockManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(s_stocks stockDB, Stock stock)
        {
            stock.Symbol = stockDB.codigo_especie;
            stock.Name = stockDB.nombre;
            stock.Market = new Market()
            {
                Code = stockDB.mercado

            };
            stock.Category = stockDB.categoria;
            stock.Country = stockDB.pais;
        }

        private void FieldMap(Stock stock, s_stocks stockDB)
        {
            stockDB.codigo_especie = stock.Symbol;
            stockDB.nombre = stock.Name;
            stockDB.mercado = stock.Market.Code;
            stockDB.categoria = stock.Category;
            stockDB.pais = stock.Country;
        }

        private Stock Map(s_stocks stockDB)
        {
            Stock stock = new Stock();
            FieldMap(stockDB, stock);
            return stock;
        }

        private s_stocks Map(Stock stock)
        {
            s_stocks stockDB = new s_stocks();
            FieldMap(stock, stockDB);
            return stockDB;
        }

        #endregion

        #region Public Methods

        public Stock GetByCode(string symbol, string market, string country)
        {
            if (country != Market._DEFAULT_COUNTRY)
                symbol += "." + market;


            s_stocks stockDB = ctx.s_stocks.Where(x => x.codigo_especie == symbol).FirstOrDefault();

            if (stockDB != null)
            {
                Stock stock = Map(stockDB);
                return stock;
            }
            else
                return null;

        }

        public void Update(Stock stock)
        {
            s_stocks prevStockDB = ctx.s_stocks.Where(x => x.codigo_especie == stock.Symbol).FirstOrDefault();
            FieldMap(stock, prevStockDB);
            ctx.SaveChanges();
        }

        public void Persist(Stock stock)
        {
            //Insert
            s_stocks prevStockDB = ctx.s_stocks.Where (x=>x.codigo_especie==stock.Symbol).FirstOrDefault();
            if (prevStockDB == null)
            {
                s_stocks stockDB = Map(stock);
                ctx.s_stocks.AddObject(stockDB);
                ctx.SaveChanges();
            }
            else
            {
                FieldMap(stock, prevStockDB);
                ctx.SaveChanges();
            }
        }

        public List<Stock> GetByMarket(string market)
        {
            List<s_stocks> stocksDB = ctx.s_stocks.Where(x => x.mercado == market).ToList();
            List<Stock> stocks = new List<Stock>();

            foreach (s_stocks stockDB in stocksDB)
            {
                stocks.Add(Map(stockDB));
            }

            return stocks;
        }

        #endregion
    }
}

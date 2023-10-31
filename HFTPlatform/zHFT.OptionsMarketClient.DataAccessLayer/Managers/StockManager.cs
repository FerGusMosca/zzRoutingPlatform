using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.DataAccessLayer;
using zHFT.OptionsMarketClient.BusinessEntities;
using zHFT.OptionsMarketClient.DataAccess;

namespace zHFT.OptionsMarketClient.DataAccessLayer.Managers
{
    public class StockManager : MappingEnabledAbstract
    {
        #region Constructors
        public StockManager(AutPortfolioEntities context) : base(context) { }

        public StockManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(Stock stock, stocks stockDB)
        {
            stockDB.symbol = stock.Symbol;
            stockDB.name = stock.Name;
        }

        private void FieldMap(stocks stockDB, Stock stock)
        {
            stock.Symbol = stockDB.symbol;
            stock.Name = stockDB.name;
        }

        private stocks Map(Stock stock)
        {
            stocks stockDB = new stocks();
            FieldMap(stock, stockDB);
            return stockDB;
        }

        private Stock Map(stocks stockDB)
        {
            Stock stock = new Stock();
            FieldMap(stockDB, stock);
            return stock;
        }

        #endregion

        #region Public Methods

        public List<Stock> GetAll()
        {
            List<Stock> stocks = new List<Stock>();
            List<stocks> stocksDB = ctx.stocks.ToList();

            foreach (stocks stockDB in stocksDB)
            {
                stocks.Add(Map(stockDB));
            }

            return stocks;
        }


        #endregion
    }
}

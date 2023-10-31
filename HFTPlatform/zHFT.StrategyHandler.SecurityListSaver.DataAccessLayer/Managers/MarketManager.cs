using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.SecurityListSaver.BusinessEntities;
using zHFT.StrategyHandler.SecurityListSaver.DataAccess;

namespace zHFT.StrategyHandler.SecurityListSaver.DataAccessLayer.Managers
{
    public class MarketManager : MappingEnabledAbstract
    {
        #region Constructors

        public MarketManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(markets marketDB, Market market)
        {
            market.Code = marketDB.code;
            market.Country = marketDB.country;
            market.Id = marketDB.id;
            market.Name = marketDB.name;
        }

        private Market Map(markets marketDB)
        {
            Market market = new Market();
            FieldMap(marketDB, market);
            return market;
        }

        #endregion

        #region Public Methods

        public Market GetByCode(string code)
        {
            markets marketDB = ctx.markets.Where(x => x.code == code).FirstOrDefault();

            if (marketDB != null)
            {
                Market market = Map(marketDB);
                return market;
            }
            else
                return null;
           
        }

        #endregion

    }
}

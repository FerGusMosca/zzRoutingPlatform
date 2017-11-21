
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccess;


namespace zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccessLayer
{
    public class MappingEnabledAbstract
    {
        #region Protected Methods
        protected readonly StocksHistoricalDataEntities ctx;
        #endregion

        #region Constructors
        public MappingEnabledAbstract(StocksHistoricalDataEntities context)
        {
            ctx = context;
            //AutoMapperConfiguration.Instance.Configure();
        }



        public MappingEnabledAbstract(string connectionString)
        {
            ctx = DataContextFactory.GetSecuritiesHistoricalDataContext(connectionString);
            //AutoMapperConfiguration.Instance.Configure();
        }


        #endregion
    }
}

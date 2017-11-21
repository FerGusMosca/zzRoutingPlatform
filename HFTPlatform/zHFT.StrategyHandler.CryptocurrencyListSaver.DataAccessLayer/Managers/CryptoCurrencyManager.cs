using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntitie;
using zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccess;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccessLayer.Managers
{
    public class CryptoCurrencyManager : MappingEnabledAbstract
    {
        #region Constructors

        public CryptoCurrencyManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(s_cryptocurrencies cryptoCurrDB, CryptoCurrency cryptoCurr)
        {
            cryptoCurr.BaseAddress = cryptoCurrDB.base_address;
            cryptoCurr.CoinType = cryptoCurrDB.coin_type;
            cryptoCurr.Symbol = cryptoCurrDB.symbol;
            cryptoCurr.Name = cryptoCurrDB.name;
            cryptoCurr.IsActive = cryptoCurrDB.active.HasValue ? cryptoCurrDB.active.Value : false;
            cryptoCurr.Notice = cryptoCurrDB.notice;

        }

        private void FieldMap(CryptoCurrency cryptoCurr, s_cryptocurrencies cryptoCurrDB)
        {
            cryptoCurrDB.base_address = cryptoCurr.BaseAddress;
            cryptoCurrDB.coin_type = cryptoCurr.CoinType;
            cryptoCurrDB.symbol = cryptoCurr.Symbol;
            cryptoCurrDB.name = cryptoCurr.Name;
            cryptoCurrDB.active = cryptoCurr.IsActive;
            cryptoCurrDB.notice = cryptoCurr.Notice;
        }

        private CryptoCurrency Map(s_cryptocurrencies cryptoCurrDB)
        {
            CryptoCurrency cryptoCurr = new CryptoCurrency();
            FieldMap(cryptoCurrDB, cryptoCurr);
            return cryptoCurr;
        }

        private s_cryptocurrencies Map(CryptoCurrency cryptoCurr)
        {
            s_cryptocurrencies cryptoCurrDB = new s_cryptocurrencies();
            FieldMap(cryptoCurr, cryptoCurrDB);
            return cryptoCurrDB;
        }

        #endregion

        #region Public Methods

        public void Persist(CryptoCurrency cryptoCurr)
        {
            //Insert
            s_cryptocurrencies cryptoCurrDB = ctx.s_cryptocurrencies.Where(x => x.symbol == cryptoCurr.Symbol).FirstOrDefault();
            if (cryptoCurrDB == null)
            {
                cryptoCurrDB = Map(cryptoCurr);
                ctx.s_cryptocurrencies.AddObject(cryptoCurrDB);
                ctx.SaveChanges();
            }
            else
            {
                FieldMap(cryptoCurr, cryptoCurrDB);
                ctx.SaveChanges();
            }
        }

        #endregion
    }
}

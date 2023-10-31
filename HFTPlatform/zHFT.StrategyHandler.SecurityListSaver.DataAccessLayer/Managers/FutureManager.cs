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
    public class FutureManager : MappingEnabledAbstract
    { 
        #region Constructors

        public FutureManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(s_futures futureDB, Future future)
        {
            future.Id = futureDB.id;
            future.Symbol = futureDB.symbol;
            future.UnderlyingSymbol = futureDB.underlying_symbol;
            future.SecurityDesc = futureDB.security_desc;
            future.MaturityMonthYear = futureDB.maturity_month_year;
            future.MaturityDate = futureDB.maturity_date;
            future.Factor = Convert.ToDouble(futureDB.factor);
            future.ContractMultiplier = Convert.ToDouble(futureDB.contract_multiplier);
            future.Currency = futureDB.currency;
            future.Exchange = futureDB.security_exchange;
            future.Expired = futureDB.expired;
           
        }

        private void FieldMap(Future future, s_futures futureDB)
        {
            futureDB.id = future.Id;
            futureDB.symbol = future.Symbol;
            futureDB.underlying_symbol = future.UnderlyingSymbol;
            futureDB.security_desc = future.SecurityDesc;
            futureDB.maturity_month_year = future.MaturityMonthYear;
            futureDB.maturity_date = future.MaturityDate.HasValue ? future.MaturityDate.Value : DateTime.Now.Date;
            futureDB.factor = future.Factor.HasValue ? Convert.ToDecimal(future.Factor.Value) : 0;
            futureDB.contract_multiplier = future.ContractMultiplier.HasValue ? Convert.ToDecimal(future.ContractMultiplier.Value) : 1;
            futureDB.currency = future.Currency;
            futureDB.security_exchange = future.Exchange;
            futureDB.expired = future.Expired;
        }

        private void FieldMap(Security future, s_futures futureDB)
        {
            futureDB.symbol = future.Symbol;
            futureDB.underlying_symbol = future.UnderlyingSymbol;
            futureDB.security_desc = future.SecurityDesc;
            futureDB.maturity_month_year = future.MaturityMonthYear;
            futureDB.maturity_date = future.MaturityDate.HasValue ? future.MaturityDate.Value : DateTime.Now.Date;
            futureDB.factor = future.Factor.HasValue ? Convert.ToDecimal(future.Factor.Value) : 0;
            futureDB.contract_multiplier = future.ContractMultiplier.HasValue ? Convert.ToDecimal(future.ContractMultiplier.Value) : 1;
            futureDB.currency = future.Currency;
            futureDB.security_exchange = future.Exchange;
            futureDB.expired = false;
        }

        private Future Map(s_futures futureDB)
        {
            Future future = new Future();
            FieldMap(futureDB, future);
            return future;
        }

        private s_futures Map(Future future)
        {
            s_futures futureDB = new s_futures();
            FieldMap(future, futureDB);
            return futureDB;
        }

        private s_futures Map(Security future)
        {
            s_futures futureDB = new s_futures();
            FieldMap(future, futureDB);
            return futureDB;
        }

        #endregion

        #region Public Methods

        public Future GetBySymbol(string symbol, string market)
        {

            s_futures futureDB = ctx.s_futures.Where(x => x.symbol == symbol && x.security_exchange == market).OrderByDescending(x => x.maturity_date).FirstOrDefault();

            if (futureDB != null)
            {
                Future future = Map(futureDB);
                return future;
            }
            else
                return null;

        }

        public Future GetFutureByPrefix(string symbolPrefix, string market)
        {

            s_futures futureDB = ctx.s_futures.Where(x => x.symbol.StartsWith(symbolPrefix) && x.security_exchange == market).OrderByDescending(x => x.maturity_date).FirstOrDefault();

            if (futureDB != null)
            {
                Future future = Map(futureDB);
                return future;
            }
            else
                return null;

        }

        public List<Future> GetByMarket(string market)
        {
            List<s_futures> futuresDB = ctx.s_futures.Where(x => x.security_exchange == market && x.expired == false).ToList();
            List<Future> futures = new List<Future>();

            foreach (s_futures futureDB in futuresDB)
            {
                futures.Add(Map(futureDB));
            }

            return futures;
        }

        public void Persist(Future future)
        {
            //Insert
            s_futures prevFutureDB = ctx.s_futures.Where(x => x.symbol == future.Symbol).FirstOrDefault();
            if (prevFutureDB == null)
            {
                s_futures futureDB = Map(future);
                ctx.s_futures.AddObject(futureDB);
                ctx.SaveChanges();
            }
            else
            {
                FieldMap(future, prevFutureDB);
                ctx.SaveChanges();
            }
        }

        public void Insert(Security future)
        {
            s_futures futureDB = Map(future);
            ctx.s_futures.AddObject(futureDB);
            ctx.SaveChanges();
        }


        #endregion
    }
}

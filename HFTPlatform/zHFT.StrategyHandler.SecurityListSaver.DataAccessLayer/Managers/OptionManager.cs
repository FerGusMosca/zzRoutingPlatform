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
    public class OptionManager : MappingEnabledAbstract
    {
        #region Constructors

        public OptionManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(s_options optionDB, Option option)
        {
            option.Id = optionDB.id;
            option.Symbol = optionDB.symbol;
            option.SymbolSfx = optionDB.symbol_sfx;
            option.StrikePrice = Convert.ToDouble(optionDB.strike_price);
            option.PutOrCall = optionDB.put_or_call;
            option.StrikeMultiplier = optionDB.strike_multiplier;
            option.StrikeCurrency = optionDB.strike_currency;
            option.MaturityMonthYear = optionDB.maturity_month_year;
            option.MaturityDate = optionDB.maturity_date;
            option.StrikeCurrency = optionDB.currency;
            option.Exchange = optionDB.security_exchange;
         
        }

        private void FieldMap(Option option, s_options optionDB)
        {
            optionDB.id = option.Id;
            optionDB.symbol = option.Symbol;
            optionDB.symbol_sfx = option.SymbolSfx;
            optionDB.strike_price = Convert.ToDecimal(option.StrikePrice);
            optionDB.put_or_call = option.PutOrCall;
            optionDB.strike_multiplier = option.StrikeMultiplier;
            optionDB.strike_currency = option.StrikeCurrency;
            optionDB.maturity_month_year = option.MaturityMonthYear;
            optionDB.maturity_date = option.MaturityDate.HasValue ? option.MaturityDate.Value : DateTime.Now.Date;
            optionDB.strike_currency = option.StrikeCurrency;
            optionDB.security_exchange = option.Exchange;
        }

        private void FieldMap(Security option, s_options optionDB)
        {
            optionDB.symbol = option.Symbol;
            optionDB.symbol_sfx = option.SymbolSfx;
            optionDB.strike_price = Convert.ToDecimal(option.StrikePrice);
            optionDB.put_or_call = option.PutOrCall;
            optionDB.strike_multiplier = option.StrikeMultiplier;
            optionDB.strike_currency = option.StrikeCurrency;
            optionDB.maturity_month_year = option.MaturityMonthYear;
            optionDB.maturity_date = option.MaturityDate.HasValue ? option.MaturityDate.Value : DateTime.Now.Date;
            optionDB.strike_currency = option.StrikeCurrency;
            optionDB.security_exchange = option.Exchange;
        }

        private Option Map(s_options optionDB)
        {
            Option option = new Option();
            FieldMap(optionDB, option);
            return option;
        }

        private s_options Map(Option option)
        {
            s_options optionDB = new s_options();
            FieldMap(option, optionDB);
            return optionDB;
        }

        private s_options Map(Security option)
        {
            s_options optionDB = new s_options();
            FieldMap(option, optionDB);
            return optionDB;
        }

        #endregion

        #region Public Methods

        public Option GetBySymbol(string symbol, string market)
        {

            s_options optionDB = ctx.s_options.Where(x => x.symbol == symbol && x.security_exchange==market).OrderByDescending(x=>x.maturity_date).FirstOrDefault();

            if (optionDB != null)
            {
                Option option = Map(optionDB);
                return option;
            }
            else
                return null;

        }

        public Option GetOptionByPrefix(string symbolPrefix, string market)
        {

            s_options optionDB = ctx.s_options.Where(x => x.symbol.StartsWith(symbolPrefix) && x.security_exchange==market).OrderByDescending(x => x.maturity_date).FirstOrDefault();

            if (optionDB != null)
            {
                Option option = Map(optionDB);
                return option;
            }
            else
                return null;

        }

        public List<Option> GetByMarket(string market)
        {
            List<s_options> optionsDB = ctx.s_options.Where(x => x.security_exchange == market && x.expired==false).ToList();
            List<Option> options = new List<Option>();

            foreach (s_options optionDB in optionsDB)
            {
                options.Add(Map(optionDB));
            }

            return options;
        }

        public void Persist(Option option)
        {
            //Insert
            s_options prevOptionDB = ctx.s_options.Where(x => x.symbol == option.Symbol).FirstOrDefault();
            if (prevOptionDB == null)
            {
                s_options optionDB = Map(option);
                ctx.s_options.AddObject(optionDB);
                ctx.SaveChanges();
            }
            else
            {
                FieldMap(option, prevOptionDB);
                ctx.SaveChanges();
            }
        }

        public void Insert(Security option)
        {
            s_options optionDB = Map(option);
            ctx.s_options.AddObject(optionDB);
            ctx.SaveChanges();
        }

        #endregion
    }
}

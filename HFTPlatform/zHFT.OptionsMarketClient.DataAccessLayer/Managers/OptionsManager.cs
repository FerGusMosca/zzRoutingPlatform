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
    public class OptionsManager : MappingEnabledAbstract
    {
        #region Constructors
        public OptionsManager(AutPortfolioEntities context) : base(context) { }

        public OptionsManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(Option option, options optionDB)
        {
            optionDB.symbol = option.Symbol;
            optionDB.symbol_sfx = option.SymbolSfx;
            optionDB.strike_multiplier = option.StrikeMultiplier;
            optionDB.put_or_call = Convert.ToChar(option.PutOrCall).ToString();
            optionDB.strike_price = Convert.ToDecimal(option.StrikePrice);
            optionDB.strike_currency = option.StrikeCurrency;
            optionDB.maturity_month_year = option.MaturityMonthYear;
            optionDB.maturity_date = option.MaturityDate;
            optionDB.currency = option.Currency;
            optionDB.security_exchange = option.SecurityExchange;
            optionDB.expired = option.Expired;
        }

        private void FieldMap(options optionDB, Option option)
        {
            option.Id = optionDB.id;
            option.Symbol = optionDB.symbol;
            option.SymbolSfx = optionDB.symbol_sfx;
            option.StrikeMultiplier = optionDB.strike_multiplier;
            option.PutOrCall = (PutOrCall)Convert.ToChar(optionDB.put_or_call);
            option.StrikePrice = Convert.ToDouble(optionDB.strike_price);
            option.StrikeCurrency = optionDB.strike_currency;
            option.MaturityMonthYear = optionDB.maturity_month_year;
            option.MaturityDate = optionDB.maturity_date;
            option.Currency = optionDB.currency;
            option.SecurityExchange = optionDB.security_exchange;
            option.Expired = optionDB.expired;
        }

        private options Map(Option option)
        {
            options optionsDB = new options();
            FieldMap(option, optionsDB);
            return optionsDB;
        }

        private Option Map(options optionsDB)
        {
            Option option = new Option();
            FieldMap(optionsDB, option);
            return option;
        }

        #endregion

        #region Public Methods

        public List<Option> GetAllOptionsBySymbol(string symbol)
        {
            List<Option> options = new List<Option>();
            List<options> optionsDB = ctx.options.Where(x => x.symbol_sfx == symbol
                                                            && x.expired == false).ToList();

            foreach (options optionDB in optionsDB)
            {
                options.Add(Map(optionDB));
            }

            return options;
        }


        public List<Option> GetAllActiveOptionsBySymbol(string symbol)
        {
            List<Option> options = new List<Option>();
            List<options> optionsDB = ctx.options.Where(x =>    x.symbol_sfx==symbol 
                                                            && DateTime.Compare(DateTime.Now,x.maturity_date)<0
                                                            &&  x.expired == false).ToList();

            foreach (options optionDB in optionsDB)
            {
                options.Add(Map(optionDB));
            }

            return options;
        }

        public Option GetActiveOptionBySymbol(string symbol)
        {
            options optionsDB = ctx.options.Where(x => !x.expired && string.Compare(x.symbol, symbol) == 0)
                                           .FirstOrDefault();

            return optionsDB != null ? Map(optionsDB) : null;
        }


        public void Update(Option option)
        {
            options optionDB = ctx.options.ToList().Where(x => x.id == option.Id).FirstOrDefault();
            FieldMap(option, optionDB);
            ctx.SaveChanges();
        }

        public void Persist(Option option)
        {
            //Insert
            if (option.Id == 0)
            {
                options optionDB = Map(option);
                ctx.options.AddObject(optionDB);
                ctx.SaveChanges();
                option.Id = optionDB.id;
            }
            else
                Update(option);
        }


        #endregion
    }
}

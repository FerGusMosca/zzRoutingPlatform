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
    public class DailyOptionManager : MappingEnabledAbstract
    {
        #region Constructors
        public DailyOptionManager(AutPortfolioEntities context) : base(context) { }

        public DailyOptionManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(DailyOption dailyOption, daily_options dailyOptionDB)
        {
            dailyOptionDB.symbol = dailyOption.Option.Symbol;
            dailyOptionDB.symbol_sfx = dailyOption.Option.SymbolSfx;
            dailyOptionDB.date = dailyOption.Date;
            dailyOptionDB.processed = dailyOption.Processed;
        }

        private void FieldMap(daily_options dailyOptionDB, DailyOption dailyOption)
        {
            dailyOption.Option = new Option()
                                            {
                                                Symbol = dailyOptionDB.symbol,
                                                SymbolSfx=dailyOptionDB.symbol_sfx
                                            };
            dailyOption.Date = dailyOptionDB.date;
            dailyOption.Processed = dailyOptionDB.processed;
        
        }

        private daily_options Map(DailyOption dailyOption)
        {
            daily_options dailyOptionsDB = new daily_options();
            FieldMap(dailyOption, dailyOptionsDB);
            return dailyOptionsDB;
        }

        private DailyOption Map(daily_options dailyOptionsDB)
        {
            DailyOption dailyOption = new DailyOption();
            FieldMap(dailyOptionsDB, dailyOption);
            return dailyOption;
        }

        #endregion

        #region Public Methods

        private daily_options GetByKey(string symbol, DateTime date)
        {
            return ctx.daily_options.Where(x => x.symbol == symbol && DateTime.Compare(x.date, date) == 0).FirstOrDefault();

        }

        public DailyOption GetBySymbolAndDate(string symbol, DateTime date)
        {
            daily_options dailyOptDB = GetByKey(symbol, date);

            if (dailyOptDB != null)
                return Map(dailyOptDB);
            else
                return null;

        }

        public List<DailyOption> GetLatestToProcess(DateTime date)
        {
            List<DailyOption> dailyOptions = new List<DailyOption>();
            List<daily_options> dailyOptionsDB = ctx.daily_options.Where(x =>   DateTime.Compare(x.date, date.Date) == 0
                                                                             && x.processed).ToList();


            foreach (daily_options dailyOptionDB in dailyOptionsDB)
            {
                dailyOptions.Add(Map(dailyOptionDB));
            }

            return dailyOptions;
        }

        public List<DailyOption> GetManualContracts(DateTime date)
        {
            List<DailyOption> dailyOptions = new List<DailyOption>();
            List<daily_options> dailyOptionsDB = ctx.daily_options.Where(x => DateTime.Compare(x.date, date.Date) == 0
                                                                             && !x.processed).ToList();


            foreach (daily_options dailyOptionDB in dailyOptionsDB)
            {
                dailyOptions.Add(Map(dailyOptionDB));
            }

            return dailyOptions;
        }

        public void Update(DailyOption dailyOpt)
        {
            daily_options dailyOptionsDB = GetByKey(dailyOpt.Option.Symbol, dailyOpt.Date);
            FieldMap(dailyOpt, dailyOptionsDB);
            ctx.SaveChanges();
        }

        public void Persist(DailyOption dailyOpt)
        {
            //Insert
            if (GetByKey(dailyOpt.Option.Symbol,dailyOpt.Date)==null)
            {
                daily_options dailyOptionDB = Map(dailyOpt);
                ctx.daily_options.AddObject(dailyOptionDB);
                ctx.SaveChanges();
            }
            else
                Update(dailyOpt);
        }


        #endregion
    }
}

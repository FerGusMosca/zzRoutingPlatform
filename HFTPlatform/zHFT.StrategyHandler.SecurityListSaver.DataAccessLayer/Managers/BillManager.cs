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
    public class BillManager : MappingEnabledAbstract
    {
        #region Constructors

        public BillManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(s_bills billDB, Bill bill)
        {
            bill.Id = billDB.id;
            bill.Symbol = billDB.symbol;
            bill.SecurityDesc = billDB.security_desc;
            bill.MaturityMonthYear = billDB.maturity_month_year;
            bill.MaturityDate = billDB.maturity_date;
            bill.Factor = Convert.ToDouble(billDB.factor);
            bill.ContractMultiplier = Convert.ToDouble(billDB.contract_multiplier);
            bill.Currency = billDB.currency;
            bill.Exchange = billDB.security_exchange;
            bill.Expired = billDB.expired;

        }

        private void FieldMap(Bill bill, s_bills billDB)
        {
            billDB.id = bill.Id;
            billDB.symbol = bill.Symbol;
            billDB.security_desc = bill.SecurityDesc;
            billDB.maturity_month_year = bill.MaturityMonthYear;
            billDB.maturity_date = bill.MaturityDate.HasValue ? bill.MaturityDate.Value : DateTime.Now.Date;
            billDB.factor = bill.Factor.HasValue ? Convert.ToDecimal(bill.Factor.Value) : 0;
            billDB.contract_multiplier = bill.ContractMultiplier.HasValue ? Convert.ToDecimal(bill.ContractMultiplier.Value) : 1;
            billDB.currency = bill.Currency;
            billDB.security_exchange = bill.Exchange;
            billDB.expired = bill.Expired;
        }

        private void FieldMap(Security bill, s_bills billDB)
        {
            billDB.symbol = bill.Symbol;
            billDB.security_desc = bill.SecurityDesc;
            billDB.maturity_month_year = bill.MaturityMonthYear;
            billDB.maturity_date = bill.MaturityDate.HasValue ? bill.MaturityDate.Value : DateTime.Now.Date;
            billDB.factor = bill.Factor.HasValue ? Convert.ToDecimal(bill.Factor.Value) : 0;
            billDB.contract_multiplier = bill.ContractMultiplier.HasValue ? Convert.ToDecimal(bill.ContractMultiplier.Value) : 1;
            billDB.currency = bill.Currency;
            billDB.security_exchange = bill.Exchange;
            billDB.expired = false;
        }

        private Bill Map(s_bills billDB)
        {
            Bill bill = new Bill();
            FieldMap(billDB, bill);
            return bill;
        }

        private s_bills Map(Bill bill)
        {
            s_bills billDB = new s_bills();
            FieldMap(bill, billDB);
            return billDB;
        }

        private s_bills Map(Security bill)
        {
            s_bills billDB = new s_bills();
            FieldMap(bill, billDB);
            return billDB;
        }

        #endregion

        #region Public Methods

        public Bill GetBySymbol(string symbol, string market)
        {

            s_bills billDB = ctx.s_bills.Where(x =>    x.symbol == symbol 
                                                    && x.security_exchange == market)
                                        .OrderByDescending(x => x.maturity_date).FirstOrDefault();

            if (billDB != null)
            {
                Bill bill = Map(billDB);
                return bill;
            }
            else
                return null;

        }

        public Bill GetBillByPrefix(string symbolPrefix, string market)
        {

            s_bills billDB = ctx.s_bills.Where(x =>    x.symbol.StartsWith(symbolPrefix) 
                                                    && x.security_exchange == market)
                                        .OrderByDescending(x => x.maturity_date).FirstOrDefault();

            if (billDB != null)
            {
                Bill bill = Map(billDB);
                return bill;
            }
            else
                return null;

        }

        public List<Bill> GetByMarket(string market)
        {
            List<s_bills> billsDB = ctx.s_bills.Where(x => x.security_exchange == market 
                                                        && x.expired == false).ToList();
            List<Bill> bills = new List<Bill>();

            foreach (s_bills billDB in billsDB)
            {
                bills.Add(Map(billDB));
            }

            return bills;
        }

        public void Persist(Bill bill)
        {
            //Insert
            s_bills prevbillDB = ctx.s_bills.Where(x => x.symbol == bill.Symbol).FirstOrDefault();

            if (prevbillDB == null)
            {
                s_bills billDB = Map(bill);
                ctx.s_bills.AddObject(billDB);
                ctx.SaveChanges();
            }
            else
            {
                FieldMap(bill, prevbillDB);
                ctx.SaveChanges();
            }
        }

        public void Insert(Security bill)
        {
            s_bills billDB = Map(bill);
            ctx.s_bills.AddObject(billDB);
            ctx.SaveChanges();
        }

        #endregion
    }
}

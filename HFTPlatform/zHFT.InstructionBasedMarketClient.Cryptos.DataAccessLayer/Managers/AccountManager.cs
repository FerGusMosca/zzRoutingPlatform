using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Cryptos.DataAccess;

namespace zHFT.InstructionBasedMarketClient.Cryptos.DataAccessLayer.Managers
{
    public class AccountManager : MappingEnabledAbstract
    {
        #region Constructors

        public AccountManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(Account account, accounts accountDB)
        {
            accountDB.customer_id = account.Customer.Id;
            accountDB.account_number = account.AccountNumber;
            accountDB.broker_id = account.Broker.Id;
            accountDB.balance = account.Balance;
            accountDB.name = account.Name;
            accountDB.generic_s_number = account.GenericAccountNumber;

        }

        private void FieldMap(accounts accountDB, Account account)
        {
            account.Customer = new Customer()
            {
                Id = accountDB.customers.Id,
                DocumentType = accountDB.customers.document_type,
                DocumentNumber = accountDB.customers.document_number,
                FirstName = accountDB.customers.first_name,
                LastName = accountDB.customers.last_name
            };

            account.AccountNumber = accountDB.account_number;
            account.Broker = new Broker() { Id = accountDB.brokers.id, Code = accountDB.brokers.code, Name = accountDB.brokers.name };

            account.Id = accountDB.id;
            account.Name = accountDB.name;
            account.GenericAccountNumber = accountDB.generic_s_number;
        }

        private accounts Map(Account account)
        {
            accounts accountDB = new accounts();
            FieldMap(account, accountDB);
            return accountDB;
        }

        private Account Map(accounts accountDB)
        {
            Account account = new Account();
            FieldMap(accountDB, account);
            return account;
        }

        #endregion

        #region Public Methods


        public Account GetById(int id)
        {
            accounts accountDB = ctx.accounts.Where(x => x.id == id).FirstOrDefault();

            Account account = Map(accountDB);

            return account;
        }

        public Account GetByAccountNumber(int accountNumber)
        {
            accounts accountDB = ctx.accounts.Where(x => x.account_number == accountNumber).FirstOrDefault();

            Account account = Map(accountDB);

            return account;
        }

        public List<Account> GetByFilters(long? accountNumber, long? documentNumber, string name, Int32 pagenumber, Int32 pagesize, ref int totalRecords)
        {
            List<Account> accounts = new List<Account>();
            List<accounts> accountsDB = ctx.accounts.Where(x => ((documentNumber.HasValue && x.customers.document_number == documentNumber.Value)
                                                                        || (accountNumber.HasValue && x.account_number == accountNumber.Value)
                                                                 )
                                                               || (!string.IsNullOrEmpty(name) && x.customers.last_name.Contains(name))).ToList();

            totalRecords = accountsDB.Count;

            foreach (accounts accounDB in accountsDB.Skip((pagenumber - 1) * pagesize).Take(pagesize))
            {
                accounts.Add(Map(accounDB));
            }

            return accounts;
        }

        public void Update(Account account)
        {
            accounts accountDB = ctx.accounts.ToList().Where(x => x.id == account.Id).FirstOrDefault();
            FieldMap(account, accountDB);
            ctx.SaveChanges();
        }

        public void Delete(Account account)
        {
            accounts accountDB = ctx.accounts.Where(x => x.id == account.Id).FirstOrDefault();
            ctx.accounts.DeleteObject(accountDB);
            ctx.SaveChanges();
        }

        public void Persist(Account account)
        {
            //Insert
            if (account.Id == 0)
            {
                accounts accountDB = Map(account);
                ctx.accounts.AddObject(accountDB);
                ctx.SaveChanges();
                account.Id = accountDB.id;
            }
            else
                Update(account);
        }

        #endregion

    }
}

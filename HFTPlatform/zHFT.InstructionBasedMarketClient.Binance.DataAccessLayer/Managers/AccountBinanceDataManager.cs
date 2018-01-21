using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.Binance.BusinessEntities;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Cryptos.DataAccess;

namespace zHFT.InstructionBasedMarketClient.Binance.DataAccessLayer.Managers
{
    public class AccountBinanceDataManager : MappingEnabledAbstract
    {
        #region Constructors


        public AccountBinanceDataManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(AccountBinanceData binanceData, accounts_binance_data binanceDataDB)
        {
            binanceDataDB.account_id = binanceData.Account.Id;
            binanceDataDB.api_key = binanceData.APIKey;
            binanceDataDB.secret = binanceData.Secret;

        }

        private void FieldMap(accounts_binance_data binanceDataDB, AccountBinanceData binanceData)
        {
            binanceData.Account = new Account() { Id = binanceDataDB.account_id };
            binanceData.APIKey = binanceDataDB.api_key;
            binanceData.Secret = binanceDataDB.secret;
        }

        private accounts_binance_data Map(AccountBinanceData binanceData)
        {
            accounts_binance_data binanceDataDB = new accounts_binance_data();
            FieldMap(binanceData, binanceDataDB);
            return binanceDataDB;
        }

        private AccountBinanceData Map(accounts_binance_data binanceDataDB)
        {
            AccountBinanceData binanceData = new AccountBinanceData();
            FieldMap(binanceDataDB, binanceData);
            return binanceData;
        }

        #endregion

        #region Public Methods

        public AccountBinanceData GetByAccount(Account account)
        {
            accounts_binance_data binanceDataDB = ctx.accounts_binance_data.Where(x => x.account_id == account.Id).FirstOrDefault();

            if (binanceDataDB != null)
            {
                AccountBinanceData binanceData = Map(binanceDataDB);
                binanceData.Account = account;
                return binanceData;
            }
            else
                return null;
        }

        public AccountBinanceData GetByAccountNumber(Account account)
        {
            accounts_binance_data binanceDataDB = ctx.accounts_binance_data.Where(x => x.accounts.account_number == account.AccountNumber).FirstOrDefault();

            if (binanceDataDB != null)
            {
                AccountBinanceData binanceData = Map(binanceDataDB);
                binanceData.Account = account;
                return binanceData;
            }
            else
                return null;
        }

        #endregion
    }
}

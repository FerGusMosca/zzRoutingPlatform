using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.Bittrex.BusinessEntities;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.InstructionBasedMarketClient.Cryptos.DataAccess;

namespace zHFT.InstructionBasedMarketClient.Bittrex.DataAccessLayer.Managers
{
    public class AccountBittrexDataManager : MappingEnabledAbstract
    {
        #region Constructors


        public AccountBittrexDataManager(string connectionString)
            : base(connectionString)
        {

        }
        #endregion

        #region Private Methods

        private void FieldMap(AccountBittrexData bittrexData, accounts_bittrex_data bittrexDataDB)
        {
            bittrexDataDB.account_id = bittrexData.Account.Id;
            bittrexDataDB.api_key = bittrexData.APIKey;
            bittrexDataDB.secret = bittrexData.Secret;

        }

        private void FieldMap(accounts_bittrex_data bittrexDataDB, AccountBittrexData bittrexData)
        {
            bittrexData.Account = new Account() { Id = bittrexDataDB.account_id };
            bittrexData.APIKey = bittrexDataDB.api_key;
            bittrexData.Secret = bittrexDataDB.secret;
        }

        private accounts_bittrex_data Map(AccountBittrexData bittrexData)
        {
            accounts_bittrex_data bittrexDataDB = new accounts_bittrex_data();
            FieldMap(bittrexData, bittrexDataDB);
            return bittrexDataDB;
        }

        private AccountBittrexData Map(accounts_bittrex_data bittrexDataDB)
        {
            AccountBittrexData bittrexData = new AccountBittrexData();
            FieldMap(bittrexDataDB, bittrexData);
            return bittrexData;
        }

        #endregion

        #region Public Methods

        public AccountBittrexData GetByAccount(Account account)
        {
            accounts_bittrex_data bittrexDataDB = ctx.accounts_bittrex_data.Where(x => x.account_id == account.Id).FirstOrDefault();

            if (bittrexDataDB != null)
            {
                AccountBittrexData bittrexData = Map(bittrexDataDB);
                bittrexData.Account = account;
                return bittrexData;
            }
            else
                return null;
        }

        public AccountBittrexData GetByAccountNumber(Account account)
        {
            accounts_bittrex_data bittrexDataDB = ctx.accounts_bittrex_data.Where(x => x.accounts.account_number == account.AccountNumber).FirstOrDefault();

            if (bittrexDataDB != null)
            {
                AccountBittrexData bittrexData = Map(bittrexDataDB);
                bittrexData.Account = account;
                return bittrexData;
            }
            else
                return null;
        }

        #endregion
    }
}

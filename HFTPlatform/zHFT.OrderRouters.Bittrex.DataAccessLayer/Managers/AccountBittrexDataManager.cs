using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.Bittrex.DataAccessLayer;
using zHFT.OrderRouters.Bittrex.BusinessEntities;
using zHFT.OrderRouters.Cryptos.DataAccess;

namespace zHFT.OrderRouters.Bittrex.DataAccessLayer.Managers
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
            bittrexDataDB.account_id = bittrexData.AccountId;
            bittrexDataDB.api_key = bittrexData.APIKey;
            bittrexDataDB.secret = bittrexData.Secret;

        }

        private void FieldMap(accounts_bittrex_data bittrexDataDB, AccountBittrexData bittrexData)
        {
            bittrexData.AccountId = bittrexDataDB.account_id;
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

        public AccountBittrexData GetByAccountNumber(int accountNumber)
        {
            accounts_bittrex_data bittrexDataDB = ctx.accounts_bittrex_data.Where(x => x.accounts.account_number == accountNumber).FirstOrDefault();

            if (bittrexDataDB != null)
            {
                AccountBittrexData bittrexData = Map(bittrexDataDB);
                return bittrexData;
            }
            else
                return null;
        }

        #endregion
    }
}

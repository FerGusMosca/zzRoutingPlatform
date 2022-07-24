using System;
using System.Data.SqlClient;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers.ADO;

namespace zHFT.StrategyHandler.IBR.Cryptos.DataAccessLayer.Managers.ADO
{
    public class ADOCryptoAccountManager:ADOAccountManager
    {
        public ADOCryptoAccountManager(string pConnectionString) : base(pConnectionString)
        {
        }
        
        #region Protected Methods

        protected override Account BuildAccount(SqlDataReader reader)
        {

            Account account = new Account()
            {
                Id= Convert.ToInt32(reader["id"]),
                Customer = new Customer(){Id = Convert.ToInt32(reader["customer_id"])},
                AccountNumber = Convert.ToInt64(reader["account_number"]),
                Broker = new Broker(){Id=Convert.ToInt32(reader["broker_id"])},
                Name = reader["name"]!=DBNull.Value ?reader["name"].ToString():null,
                Balance = reader["balance"]!=DBNull.Value ?(long?)Convert.ToDecimal(reader["balance"]):null,
                GenericAccountNumber = reader["generic_s_number"]!=DBNull.Value ?reader["generic_s_number"].ToString():null,
            };

            return account;
        }

        #endregion
    }
}
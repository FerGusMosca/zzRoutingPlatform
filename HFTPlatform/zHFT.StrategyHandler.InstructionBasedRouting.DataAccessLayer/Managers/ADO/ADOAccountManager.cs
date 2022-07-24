using System;
using System.Data;
using System.Data.SqlClient;
using zHFT.Main.DataAccessLayer.Managers.ADO;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers.ADO
{
    public class ADOAccountManager: ADOBaseManager,IAccountManagerAccessLayer
    {
        #region Constructors

        public ADOAccountManager(string pConnectionString)
        {
            ConnectionString = pConnectionString;
        }

        #endregion
        
        #region Private Static Querys

        protected static string _SP_GET_ACCOUNTS = "GetAccounts";
        
        protected static string _SP_PERSIST_ACCOUNT = "PersistAccount";
       
        #endregion
        
        #region Protected Methods

        protected virtual Account BuildAccount(SqlDataReader reader)
        {

            Account account = new Account()
            {
                Id= Convert.ToInt32(reader["id"]),
                Customer = new Customer(){Id = Convert.ToInt32(reader["customer_id"])},
                AccountNumber = Convert.ToInt64(reader["account_number"]),
                Broker = new Broker(){Id=Convert.ToInt32(reader["broker_id"])},
                Name = reader["name"]!=DBNull.Value ?reader["name"].ToString():null,
                AccountDesc = reader["account"]!=DBNull.Value ?reader["account"].ToString():null,
                URL = reader["url"]!=DBNull.Value ?reader["url"].ToString():null,
                Port = reader["port"]!=DBNull.Value ?(long?)Convert.ToInt64(reader["port"]):null,
                Balance = reader["balance"]!=DBNull.Value ?(long?)Convert.ToDecimal(reader["balance"]):null,
                Currency = reader["currency"]!=DBNull.Value ?reader["currency"].ToString():null,
                GenericAccountNumber = reader["generic_s_number"]!=DBNull.Value ?reader["generic_s_number"].ToString():null,
            };

            return account;
        }

        #endregion
        
        #region Public Methods
        public Account GetById(int id)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_GET_ACCOUNTS, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;
            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            Account account = null;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        account=BuildAccount(reader);
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return account;
        }

        public void Persist(Account account)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_PERSIST_ACCOUNT, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            //cmd.Parameters.AddWithValue("@_id", user.Id);
            //cmd.Parameters["@_id"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Id",account.Id);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Balance", account.Balance);
            cmd.Parameters["@Balance"].Direction = ParameterDirection.Input;

            SqlDataReader reader;

            cmd.Connection.Open();

            try
            {
                // Run Query
                cmd.ExecuteScalar();
            }
            finally
            {
                cmd.Connection.Close();
            }
        }
        
        #endregion
    }
}
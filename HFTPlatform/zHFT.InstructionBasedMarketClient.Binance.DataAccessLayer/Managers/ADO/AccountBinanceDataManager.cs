using System;
using System.Data;
using System.Data.SqlClient;
using zHFT.InstructionBasedMarketClient.Binance.BusinessEntities;
using zHFT.InstructionBasedMarketClient.BusinessEntities;
using zHFT.Main.DataAccessLayer.Managers.ADO;

namespace zHFT.InstructionBasedMarketClient.Binance.DataAccessLayer.Managers.ADO
{
    public class AccountBinanceDataManager : ADOBaseManager
    {
        #region Constructors

        public AccountBinanceDataManager(string pConnectionString)
        {
            ConnectionString = pConnectionString;
        }

        #endregion
        
        #region Private Static Querys

        private static string _SP_GET_ACCOUNT_BINANCE_DATA = "GetAccountBinanceData";
       
        #endregion
        
        #region Private Methods

        private AccountBinanceData BuildAccountBinanceData(SqlDataReader reader)
        {

            AccountBinanceData accountBinanceData = new AccountBinanceData()
            {
                Account = new Account() {Id = Convert.ToInt32(reader["account_id"])},
                APIKey = reader["api_key"].ToString(),
                Secret = reader["secret"].ToString()
            };

            return accountBinanceData;
        }

        #endregion
        
        #region Public Methods
        
        public AccountBinanceData GetAccountBinanceData(int accountNumber)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_GET_ACCOUNT_BINANCE_DATA, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@AccountNumber", accountNumber);
            cmd.Parameters["@AccountNumber"].Direction = ParameterDirection.Input;
            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            AccountBinanceData accBinanceData = null;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        accBinanceData=BuildAccountBinanceData(reader);
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return accBinanceData;
        }
        
        #endregion
    }
}
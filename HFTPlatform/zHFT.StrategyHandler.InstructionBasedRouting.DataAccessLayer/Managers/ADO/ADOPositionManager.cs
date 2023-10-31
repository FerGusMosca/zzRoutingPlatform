using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.DataAccessLayer.Managers.ADO;
using zHFT.StrategyHandler.InstructionBasedRouting.BusinessEntities;
using zHFT.StrategyHandler.InstructionBasedRouting.Common.Interfaces;

namespace zHFT.StrategyHandler.InstructionBasedRouting.DataAccessLayer.Managers.ADO
{
    public class ADOPositionManager: ADOBaseManager,IPositionManagerAccessLayer
    {
        
        #region Constructors

        public ADOPositionManager(string pConnectionString)
        {
            ConnectionString = pConnectionString;
        }

        #endregion
        
        #region Protected Methods

        protected virtual AccountPosition BuildAccounPosition(SqlDataReader reader)
        {

            AccountPosition accountPos = new AccountPosition()
            {
                Id = Convert.ToInt32(reader["id"]),
                Account = new Account() {Id = Convert.ToInt32(reader["account_id"])},
                Security = new Security() {Symbol = reader["symbol"].ToString()},
                Weight = reader["weight"] != DBNull.Value ? (decimal?) Convert.ToDecimal(reader["weight"]) : null,
                Shares = reader["shares"] != DBNull.Value ? (int?) Convert.ToInt32(reader["shares"]) : null,
                MarketPrice = reader["market_price"] != DBNull.Value
                    ? (decimal?) Convert.ToDecimal(reader["market_price"])
                    : null,
                Ammount = reader["ammount"] != DBNull.Value ? (decimal?) Convert.ToDecimal(reader["ammount"]) : null,
                PositionStatus = reader["ammount"] != DBNull.Value
                    ? new PositionStatus() {Code = Convert.ToChar(reader["status"])}
                    : null,
                Active = Convert.ToBoolean(reader["active"])
            };

            return accountPos;
        }

        #endregion
        
        #region Private Static Querys

        protected static string _SP_GET_POSITIONS = "GetAccountPositions";
        
        protected static string _SP_INSERT_POSITIONS= "InsertAccountPosition";
        
        protected static string _SP_DELETE_POSITIONS= "DeleteAccountPositions";
       
        #endregion
        
        public AccountPosition GetActivePositionBySymbol(string symbol, int accountId)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_GET_POSITIONS, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@AccountId", accountId);
            cmd.Parameters["@AccountId"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Symbol", symbol);
            cmd.Parameters["@Symbol"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Id", null);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;
            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            AccountPosition accountPos = null;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        accountPos=BuildAccounPosition(reader);
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return accountPos;
        }

        public AccountPosition GetById(long id)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_GET_POSITIONS, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@AccountId", null);
            cmd.Parameters["@AccountId"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Symbol", null);
            cmd.Parameters["@Symbol"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;
            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            AccountPosition accountPos = null;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        accountPos=BuildAccounPosition(reader);
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return accountPos;
        }

        public void DeleteAll(int accountId)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_DELETE_POSITIONS, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@AccountId", accountId);
            cmd.Parameters["@AccountId"].Direction = ParameterDirection.Input;

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

        public void Insert(AccountPosition pos)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_INSERT_POSITIONS, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            //cmd.Parameters.AddWithValue("@_id", user.Id);
            //cmd.Parameters["@_id"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@AccountId",pos.Account.Id);
            cmd.Parameters["@AccountId"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Symbol",pos.Security.Symbol);
            cmd.Parameters["@Symbol"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Weight",pos.Weight);
            cmd.Parameters["@Weight"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Shares",pos.Shares);
            cmd.Parameters["@Shares"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@MarketPrice",pos.MarketPrice);
            cmd.Parameters["@MarketPrice"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Ammount",pos.Ammount);
            cmd.Parameters["@Ammount"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Status",pos.PositionStatus.Code);
            cmd.Parameters["@Status"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Active",pos.Active);
            cmd.Parameters["@Active"].Direction = ParameterDirection.Input;
            

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

        public void PersistAndReplace(List<AccountPosition> positions, int accountId)
        {

            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
            {
                DeleteAll(accountId);

                foreach (AccountPosition pos in positions)
                {
                    Insert(pos);
                }
                
                scope.Complete();

            }
        }
        
        
    }
}
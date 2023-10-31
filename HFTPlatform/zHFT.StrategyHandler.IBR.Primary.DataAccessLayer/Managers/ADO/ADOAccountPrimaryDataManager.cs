using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.IBR.Primary.BusinessEntities;
using zHFT.StrategyHandler.IBR.Primary.DataAccessLayer.Managers.ADO;

namespace zHFT.StrategyHandler.IBR.Primary.DataAccessLayer.Managers
{
    public class ADOAccountPrimaryDataManager : ADOBaseManager
    {
        #region Constructores

        public ADOAccountPrimaryDataManager(string connectionString)
        {
            Conn = new SqlConnection(connectionString);
            Conn.Open();
        
        }

        #endregion

        #region Public Methods

        public AccountPrimaryData GetAccountPrimaryData(int accountNumber)
        {
            SqlCommand cmd = new SqlCommand("get_account_primary_data", Conn);

            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter param1 = cmd.Parameters.Add("@account_number", SqlDbType.Int);
            param1.Direction = ParameterDirection.Input;
            param1.Value = accountNumber;


            SqlDataReader reader = null;
            AccountPrimaryData apd = null;
            try
            {
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    apd = BuildAccountPrimaryData(reader);

                }
            }
            catch
            {
                throw;

            }
            finally
            {
                reader.Close();
            }
            return apd;
        
        }


        #endregion

        #region Private Methods

        private AccountPrimaryData BuildAccountPrimaryData(SqlDataReader reader)
        {
            AccountPrimaryData apd = new AccountPrimaryData();

            apd.AccountId = Convert.ToInt32(reader["AccountId"]);
            apd.User = reader["User"].ToString();
            apd.Password = reader["Password"].ToString();

            return apd;

        }


        #endregion

    }
}

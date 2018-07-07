using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.InstructionBasedFullMarketConnectivity.IOL.DAL.ADO
{
    public class ADOAccountIOLDataManager : ADOBaseManager
    {
        #region Constructores

        public ADOAccountIOLDataManager(string connectionString)
        {
            Conn = new SqlConnection(connectionString);
            Conn.Open();

        }

        #endregion

        #region Public Methods

        public AccountInvertirOnlineData GetAccountPrimaryData(int accountNumber)
        {
            SqlCommand cmd = new SqlCommand("get_account_iol_data", Conn);

            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter param1 = cmd.Parameters.Add("@account_number", SqlDbType.Int);
            param1.Direction = ParameterDirection.Input;
            param1.Value = accountNumber;


            SqlDataReader reader = null;
            AccountInvertirOnlineData aiold = null;
            try
            {
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    aiold = BuildAccountIolData(reader);
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
            return aiold;
        }


        #endregion

        #region Private Methods

        private AccountInvertirOnlineData BuildAccountIolData(SqlDataReader reader)
        {
            AccountInvertirOnlineData aiol = new AccountInvertirOnlineData();

            aiol.AccountId = Convert.ToInt32(reader["AccountId"]);
            aiol.User = reader["User"].ToString();
            aiol.Password = reader["Password"].ToString();

            return aiol;
        }

        #endregion
    }
}

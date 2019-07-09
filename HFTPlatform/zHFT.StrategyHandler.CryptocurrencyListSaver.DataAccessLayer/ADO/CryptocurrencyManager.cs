using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.DataAccessLayer.Managers.ADO;
using zHFT.StrategyHandler.CryptocurrencyListSaver.BusinessEntitie;

namespace zHFT.StrategyHandler.CryptocurrencyListSaver.DataAccessLayer.ADO
{
    public class CryptocurrencyManager : ADOBaseManager
    {
        #region Constructores

        public CryptocurrencyManager(string connectionString)
        {
            Conn = new SqlConnection(connectionString);
            

        }

        #endregion  

        #region Public Methods

        public void PersistCrypto(CryptoCurrency crypto)
        {
            using (SqlCommand cmd = Conn.CreateCommand())
            {
                Conn.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PersistCryptoCurrency";
                cmd.Parameters.Add(new SqlParameter("@Symbol", crypto.Symbol));
                cmd.Parameters.Add(new SqlParameter("@Name", crypto.Name));
                cmd.Parameters.Add(new SqlParameter("@IsActive", crypto.IsActive));
                cmd.Parameters.Add(new SqlParameter("@CoinType", crypto.CoinType));
                cmd.Parameters.Add(new SqlParameter("@BaseAddress", crypto.BaseAddress));
                cmd.Parameters.Add(new SqlParameter("@Notice", crypto.Notice));
                cmd.Parameters.Add(new SqlParameter("@Exchange", crypto.Exchange));
               
                cmd.ExecuteNonQuery();

                Conn.Close();
            }
             
        }

        #endregion


    }
}

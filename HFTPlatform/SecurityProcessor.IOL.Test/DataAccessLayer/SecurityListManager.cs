using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedMarketClient.IOL.Common.DTO;
using zHFT.Main.Common.Enums;

namespace SecurityProcessor.IOL.Test.DataAccessLayer
{
    public class SecurityListManager
    {
        #region Protected Attributes

        protected string ConnectionString { get; set; }

        #endregion

        #region Constructores

        public SecurityListManager(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #endregion

        #region Public Methods

        public void PersistFund(Fund fund)
        {
            SqlConnection Conn = new SqlConnection(ConnectionString);
            Conn.Open();

            try
            {
                SqlCommand insertCMD = new SqlCommand("PersistSecurity", Conn);
                insertCMD.CommandType = CommandType.StoredProcedure;

                SqlParameter symbol = insertCMD.Parameters.Add("@Symbol", SqlDbType.VarChar, 50);
                symbol.Direction = ParameterDirection.Input;
                symbol.Value = fund.simbolo;

                SqlParameter name = insertCMD.Parameters.Add("@Name", SqlDbType.VarChar,50);
                name.Direction = ParameterDirection.Input;
                name.Value = fund.descripcion;

                SqlParameter secType = insertCMD.Parameters.Add("@SecurityType", SqlDbType.VarChar, 50);
                secType.Direction = ParameterDirection.Input;
                secType.Value = SecurityType.MF.ToString();

     

                insertCMD.ExecuteNonQuery();
            }
            finally
            {
                Conn.Close();

            }

        }

        #endregion
    }
}

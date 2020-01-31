using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.StrategyHandler.MktDataDownloader.BE;

namespace zHFT.StrategyHandler.MktDataDownloader.DAL
{
    public class ADOBondManager
    {
        #region Constructores

        public ADOBondManager(string connectionString)
        {
            Conn = new SqlConnection(connectionString);
            Conn.Open();

        }

        #endregion

        #region Protected Attributes

        protected SqlConnection Conn { get; set; }

        protected string ConnectionString { get; set; }


        #endregion

        #region Private Methods

        private Bond BuildBond(SqlDataReader reader)
        {
            Bond bond = new Bond();

            bond.Symbol = reader["symbol"].ToString();
            bond.Name = reader["name"].ToString();
            bond.Market = reader["market"].ToString();
            bond.Country = reader["country"].ToString();

            return bond;
        }

        #endregion

        #region Public Methods

      
        public List<Bond> GetBonds(string market)
        {
            SqlCommand cmd = new SqlCommand("GetBonds", Conn);

            cmd.CommandType = CommandType.StoredProcedure;
            SqlParameter param2 = cmd.Parameters.Add("@Market", SqlDbType.VarChar, 50);
            param2.Direction = ParameterDirection.Input;
            param2.Value = market;

            SqlDataReader reader = null;
            List<Bond> bonds = new List<Bond>();
            try
            {
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    bonds.Add(BuildBond(reader));

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
            return bonds;
        }

        #endregion
    }
}

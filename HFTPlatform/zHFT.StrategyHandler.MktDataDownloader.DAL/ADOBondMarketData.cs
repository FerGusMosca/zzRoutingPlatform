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
    public class ADOBondMarketData
    {
        #region Constructores

        public ADOBondMarketData(string connectionString)
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

        private BondMarketData BuildBondMarketData(SqlDataReader reader)
        {
            BondMarketData bondMarketData = new BondMarketData();

            bondMarketData.Symbol = reader["symbol"].ToString();
            bondMarketData.Timestamp = reader["timestamp"].ToString();
            bondMarketData.SettlDate = reader["settl_date"].ToString();
            bondMarketData.Datetime = Convert.ToDateTime(reader["datetime"]);
            bondMarketData.LastTrade = Convert.ToDecimal(reader["last_trade"]);
            bondMarketData.BestBidPrice = Convert.ToDecimal(reader["best_bid_price"]);
            bondMarketData.BestBidSize = Convert.ToDecimal(reader["best_bid_size"]);
            bondMarketData.BestAskPrice = Convert.ToDecimal(reader["best_ask_price"]);
            bondMarketData.BestAskSize = Convert.ToDecimal(reader["best_ask_size"]);

            return bondMarketData;

        }

        #endregion


        #region Public Methods

        public List<BondMarketData> GetBondsMarketData(DateTime start, DateTime end)
        {

            SqlCommand cmd = new SqlCommand("GetBondsMarketData", Conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;

            SqlParameter param1 = cmd.Parameters.Add("@StartTime", SqlDbType.DateTime);
            param1.Direction = ParameterDirection.Input;
            param1.Value = start;


            SqlParameter param4 = cmd.Parameters.Add("@EndTime", SqlDbType.DateTime);
            param4.Direction = ParameterDirection.Input;
            param4.Value = end;

            SqlDataReader reader = null;
            List<BondMarketData> bondsMarketData = new List<BondMarketData>();
            try
            {
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    bondsMarketData.Add(BuildBondMarketData(reader));

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
            return bondsMarketData;

        }

        public int PersistBondMarketData(BondMarketData bondMarketData)
        {
            SqlCommand cmd = new SqlCommand("PersistBondMarketData", Conn);

            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter param1 = cmd.Parameters.Add("@Symbol", SqlDbType.VarChar, 50);
            param1.Direction = ParameterDirection.Input;
            param1.Value = bondMarketData.Symbol;

            SqlParameter param2 = cmd.Parameters.Add("@SettlDate", SqlDbType.VarChar, 50);
            param2.Direction = ParameterDirection.Input;
            param2.Value = bondMarketData.SettlDate;

            SqlParameter param3 = cmd.Parameters.Add("@Timestamp", SqlDbType.VarChar, 100);
            param3.Direction = ParameterDirection.Input;
            param3.Value = bondMarketData.Timestamp;

            SqlParameter param4 = cmd.Parameters.Add("@LastTrade", SqlDbType.Decimal);
            param4.Direction = ParameterDirection.Input;
            param4.Value = bondMarketData.LastTrade;

            SqlParameter param5 = cmd.Parameters.Add("@BestBidPrice", SqlDbType.Decimal);
            param5.Direction = ParameterDirection.Input;
            param5.Value = bondMarketData.BestBidPrice;

            SqlParameter param6 = cmd.Parameters.Add("@BestBidSize", SqlDbType.Decimal);
            param6.Direction = ParameterDirection.Input;
            param6.Value = bondMarketData.BestBidSize;

            SqlParameter param7 = cmd.Parameters.Add("@BestAskPrice", SqlDbType.Decimal);
            param7.Direction = ParameterDirection.Input;
            param7.Value = bondMarketData.BestAskPrice;

            SqlParameter param8 = cmd.Parameters.Add("@BestAskSize", SqlDbType.Decimal);
            param8.Direction = ParameterDirection.Input;
            param8.Value = bondMarketData.BestAskSize;

            SqlParameter param9 = cmd.Parameters.Add("@DateTime", SqlDbType.DateTime);
            param9.Direction = ParameterDirection.Input;
            param9.Value = bondMarketData.Datetime;

            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch
            {
                throw;

            }
            finally
            {

            }
        }

        #endregion
    }
}

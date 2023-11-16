using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;

namespace tph.TrendlineTurtles.DataAccessLayer
{
    public class TrendlineManager
    {
        #region Protected Contss

        public static string _GET_TRENDLINES = "GetTrendlines";
        
        #endregion
        
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion
        
        #region Constructor

        public TrendlineManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion
        
        #region Private Methods
        
        public static bool HasColumn(SqlDataReader Reader, string ColumnName) { 
            foreach (DataRow row in Reader.GetSchemaTable().Rows) { 
                if (row["ColumnName"].ToString() == ColumnName) 
                    return true; 
            } //Still here? Column not found. 
            return false; 
        }
        
        protected virtual Trendline BuildTrendline(SqlDataReader reader)
        {

            Trendline trendline = new Trendline()
            {
                Id= Convert.ToInt64(reader["id"]),
                Security = new Security(){Symbol = reader["symbol"].ToString()},
             
                StartPrice = new MarketData()
                {
                    MDEntryDate = Convert.ToDateTime(reader["start_date"]),
                    OpeningPrice = Convert.ToDouble(reader["start_price"]),
                    TradingSessionHighPrice = Convert.ToDouble(reader["start_price"]),
                    TradingSessionLowPrice = Convert.ToDouble(reader["start_price"]),
                    ClosingPrice = Convert.ToDouble(reader["start_price"]),
                },
                EndPrice = new MarketData()
                {
                    MDEntryDate = Convert.ToDateTime(reader["end_date"]),
                    OpeningPrice = Convert.ToDouble(reader["end_price"]),
                    TradingSessionHighPrice = Convert.ToDouble(reader["end_price"]),
                    TradingSessionLowPrice = Convert.ToDouble(reader["end_price"]),
                    ClosingPrice = Convert.ToDouble(reader["end_price"]),
                },
                BrokenDate = null,
                BrokenMarketPrice = null,
                BrokenTrendlinePrice = null,
                JustBroken = false,
                JustFound = false,
                Persisted = true,
                FullSlope = 0

            };

            if (reader["trendline_type"].ToString() ==Trendline._TRENDLINE_TYPE_RESISTANCE)
                trendline.TrendlineType = TrendlineType.Resistance;
            else if (reader["trendline_type"].ToString() == Trendline._TRENDLINE_TYPE_SUPPORT)
                trendline.TrendlineType = TrendlineType.Support;
            else
            {
                throw new Exception($"Could not recognize trendline type {reader["trendline_type"]}");
            }

            
            if (HasColumn(reader, "manual_new"))
                trendline.ManualNew = reader["manual_new"] != DBNull.Value ? (bool?) Convert.ToBoolean(reader["manual_new"]): null;
            
            if (HasColumn(reader, "to_disabled"))
                trendline.ToDisabled = reader["to_disabled"] != DBNull.Value ? (bool?) Convert.ToBoolean(reader["to_disabled"]): null;
            
            if (HasColumn(reader, "disabled"))
                trendline.Disabled = reader["disabled"] != DBNull.Value ? (bool?) Convert.ToBoolean(reader["disabled"]): null;


            return trendline;
        }
        
        #endregion
        
        #region Public Methods
        
        public List<Trendline> GetTrendlines()
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_GET_TRENDLINES, new SqlConnection(ADOConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            List<Trendline> trendlines = new List<Trendline>();

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        trendlines.Add(BuildTrendline(reader));
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return trendlines;
        }
        
        
        public void Persist(Trendline trendline,MonTurtlePosition monPortfpPos)
        {

            SqlConnection Conn = null;
            try
            {

                Conn = new SqlConnection(ADOConnectionString);

                using (SqlCommand cmd = Conn.CreateCommand())
                {
                    Conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PersistTrendline";
                    cmd.Parameters.Add(new SqlParameter("@Symbol", trendline.Symbol));
                    cmd.Parameters.Add(new SqlParameter("@StartDate", trendline.StartDate));
                    cmd.Parameters.Add(new SqlParameter("@EndDate", trendline.EndDate));
                    cmd.Parameters.Add(new SqlParameter("@StartPrice", trendline.StartPrice.ClosingPrice));
                    cmd.Parameters.Add(new SqlParameter("@EndPrice", trendline.EndPrice.ClosingPrice));
                    cmd.Parameters.Add(new SqlParameter("@BrokenDate", trendline.BrokenDate));
                    cmd.Parameters.Add(new SqlParameter("@BrokenMarketPrice", trendline.BrokenMarketPrice!=null?trendline.BrokenMarketPrice.ClosingPrice:null));
                    cmd.Parameters.Add(new SqlParameter("@BrokenTrendlinePrice", trendline.BrokenTrendlinePrice));
                    cmd.Parameters.Add(new SqlParameter("@CurrentTrendlinePrice", trendline.CalculateTrendPrice(monPortfpPos.GetLastCandleDate(),monPortfpPos.GetHistoricalPrices())));
                    cmd.Parameters.Add(new SqlParameter("@SlopeDegrees", trendline.GetSlopeDegrees()));
                    cmd.Parameters.Add(new SqlParameter("@TrendlineType", Convert.ToChar(trendline.TrendlineType)));
                    cmd.Parameters.Add(new SqlParameter("@ManualNew", trendline.ManualNew));
                    cmd.Parameters.Add(new SqlParameter("@Disabled", trendline.Disabled));
                    cmd.Parameters.Add(new SqlParameter("@ToDisabled", trendline.ToDisabled));
                    cmd.ExecuteNonQuery();
                }
                Conn.Dispose();
            }
            finally 
            {
                if (Conn != null)
                    Conn.Close();
            }
        }

        public void Delete(string  symbol)
        {

            SqlConnection Conn = null;
            try
            {

                Conn = new SqlConnection(ADOConnectionString);

                using (SqlCommand cmd = Conn.CreateCommand())
                {
                    Conn.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "DeleteTrendlines";
                    cmd.Parameters.Add(new SqlParameter("@Symbol", symbol));

                    cmd.ExecuteNonQuery();
                }
                Conn.Dispose();
            }
            finally
            {
                if (Conn != null)
                    Conn.Close();
            }
        }

        #endregion


    }
}
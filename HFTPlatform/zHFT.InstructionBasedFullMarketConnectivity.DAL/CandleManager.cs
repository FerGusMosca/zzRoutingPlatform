using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.Main.DataAccessLayer.Managers.ADO;

namespace zHFT.InstructionBasedFullMarketConnectivity.DAL
{
    public class CandleManager : ADOBaseManager
    {

        #region Protected Contss

        public static string _GET_CANDLES = "GetCandles";

        #endregion

        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion

        #region Constructor

        public CandleManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion

        #region Private Methods

        protected virtual MarketData BuildMarketData(SqlDataReader reader, CandleInterval interval)
        {

            MarketData marketData = new MarketData()
            {
                Security = new Security() { Symbol = reader["symbol"].ToString() },
                OpeningPrice = (double?)GetSafeDouble(reader, "open"),
                TradingSessionHighPrice = (double?)GetSafeDouble(reader, "high"),
                TradingSessionLowPrice = (double?)GetSafeDouble(reader, "low"),
                ClosingPrice = (double?)GetSafeDouble(reader, "close"),
                Trade = (double?)GetSafeDouble(reader, "trade"),
                CashVolume = (double?)GetSafeDouble(reader, "cash_volume"),
                NominalVolume = (double?)GetSafeDouble(reader, "nominal_volume"),
                MDEntryDate = (DateTime?)GetSafeDateTime(reader, "date")
            };


            return marketData;
        }

        #endregion

        #region Public Methods

        public List<MarketData> GetCandles(string symbol, CandleInterval interval, DateTime from, DateTime to)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_GET_CANDLES, new SqlConnection(ADOConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Symbol", symbol);
            cmd.Parameters["@Symbol"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Interval", CandleIntervalTranslator.GetStrInterval(interval));
            cmd.Parameters["@Interval"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@From", from);
            cmd.Parameters["@From"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@To", to);
            cmd.Parameters["@To"].Direction = ParameterDirection.Input;


            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            List<MarketData> candles = new List<MarketData>();

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        candles.Add(BuildMarketData(reader, interval));
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return candles;
        }

        public void Persist(string symbol, CandleInterval interval, MarketData md)
        {
            using (var connection = new SqlConnection(ADOConnectionString))
            {
                using (SqlCommand cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PersistCandle";
                    cmd.Parameters.Add(new SqlParameter("@Symbol", symbol));
                    cmd.Parameters.Add(new SqlParameter("@Date", md.GetReferenceDateTime()));
                    cmd.Parameters.Add(new SqlParameter("@Interval", CandleIntervalTranslator.GetStrInterval(interval)));
                    cmd.Parameters.Add(new SqlParameter("@Open", md.OpeningPrice));
                    cmd.Parameters.Add(new SqlParameter("@High", md.TradingSessionHighPrice));
                    cmd.Parameters.Add(new SqlParameter("@Low", md.TradingSessionLowPrice));
                    cmd.Parameters.Add(new SqlParameter("@Close", md.ClosingPrice));
                    cmd.Parameters.Add(new SqlParameter("@Trade", md.Trade));
                    cmd.Parameters.Add(new SqlParameter("@CashVolume", md.CashVolume));
                    cmd.Parameters.Add(new SqlParameter("@NominalVolume", md.NominalVolume));

                    cmd.ExecuteNonQuery();
                }
                connection.Dispose();
            }

        }

        #endregion


    }
}

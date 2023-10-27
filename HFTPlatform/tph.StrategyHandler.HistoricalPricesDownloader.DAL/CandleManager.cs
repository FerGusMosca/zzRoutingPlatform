using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Util;

namespace tph.StrategyHandler.HistoricalPricesDownloader.DAL
{
    public class CandleManager
    {
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion

        #region Constructor

        public CandleManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion

        #region Public Methods

        public void PersistPortfolioPositionTrade(string symbol, CandleInterval interval, MarketData md)
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

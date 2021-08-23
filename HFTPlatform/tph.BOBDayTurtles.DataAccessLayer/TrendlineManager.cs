using System;
using System.Data;
using System.Data.SqlClient;
using tph.BOBDayTurtles.BusinessEntities;

namespace tph.BOBDayTurtles.DataAccessLayer
{
    public class TrendlineManager
    {
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion
        
        #region Constructor

        public TrendlineManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion
        
        #region Public Methods
        
        
        public void Persist(Trendline trendline,MonBOBTurtlePosition monPortfpPos)
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
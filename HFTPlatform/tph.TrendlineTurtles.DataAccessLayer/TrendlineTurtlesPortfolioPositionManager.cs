using System;
using System.Data;
using System.Data.SqlClient;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;


namespace tph.TrendlineTurtles.DataAccessLayer
{
      public class TrendlineTurtlesPortfolioPositionManager
    {
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion

        #region Constructor

        public TrendlineTurtlesPortfolioPositionManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion
        
        #region Public Methods

        public void PersistPortfolioPositionTrade(TradTrendlineTurtlesPosition pos)
        {
            using (var connection = new SqlConnection(ADOConnectionString))
            {
                using (SqlCommand cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PersistDailyTurtlesTrades";
                    cmd.Parameters.Add(new SqlParameter("@StrategyName", pos.StrategyName));
                    cmd.Parameters.Add(new SqlParameter("@OpeningDate", pos.OpeningDate));
                    cmd.Parameters.Add(new SqlParameter("@ClosingDate", pos.ClosingDate));
                    cmd.Parameters.Add(new SqlParameter("@Symbol",pos.OpeningPosition.Security.Symbol ));
                    cmd.Parameters.Add(new SqlParameter("@Qty", pos.OpeningPosition.GetPositionQty()));
                    cmd.Parameters.Add(new SqlParameter("@TradeDirection", pos.TradeDirection));
                    cmd.Parameters.Add(new SqlParameter("@OpeningPrice", pos.OpeningPrice));
                    cmd.Parameters.Add(new SqlParameter("@LastPrice", pos.LastPrice));
                    cmd.Parameters.Add(new SqlParameter("@ClosingPrice", pos.ClosingPrice));
                    cmd.Parameters.Add(new SqlParameter("@TotalFee", pos.TotalFee));
                    cmd.Parameters.Add(new SqlParameter("@NominalProfit", pos.NominalProfit));
                    cmd.Parameters.Add(new SqlParameter("@FeeType", pos.FeeTypePerTrade));
                    cmd.Parameters.Add(new SqlParameter("@FeeValue", pos.FeeValuePerTrade));
                    cmd.Parameters.Add(new SqlParameter("@InitialCap", pos.InitialCap));
                    cmd.Parameters.Add(new SqlParameter("@FinalCap", pos.FinalCap));
                    
                    cmd.Parameters.Add(new SqlParameter("@TrendlineStartDate",
                        pos.OpeningTrendline != null ? (DateTime?) pos.OpeningTrendline.StartDate : null));
                    
                    cmd.Parameters.Add(new SqlParameter("@TrendlineEndDate",
                        pos.OpeningTrendline != null ? (DateTime?) pos.OpeningTrendline.EndDate : null));
                    
                    cmd.Parameters.Add(new SqlParameter("@Profit", pos.Profit));


                    cmd.ExecuteNonQuery();
                }
                connection.Dispose();
            }

        }


        #endregion
    }
}
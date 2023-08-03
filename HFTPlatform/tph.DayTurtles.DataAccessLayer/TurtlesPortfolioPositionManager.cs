using System.Data;
using System.Data.SqlClient;
using tph.DayTurtles.BusinessEntities;

namespace tph.DayTurtles.DataAccessLayer
{
    public class TurtlesPortfolioPositionManager
    {
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion

        #region Constructor

        public TurtlesPortfolioPositionManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion
        
        #region Public Methods

        public void PersistPortfolioPositionTrade(TradTurtlesPosition pos)
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
                    cmd.Parameters.Add(new SqlParameter("@Qty", pos.Qty));
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
                    cmd.Parameters.Add(new SqlParameter("@Profit", pos.Profit));

                    if (pos.IsFirstLeg())
                    {
                        cmd.Parameters.Add(new SqlParameter("@IsClosing", "N"));
                        cmd.Parameters.Add(new SqlParameter("@OpenPosId", pos.OpeningPosition.PosId));
                        cmd.Parameters.Add(new SqlParameter("@OpenPosClOrdId", pos.OpeningPosition.GetLastFilledClOrdId()));
                        cmd.Parameters.Add(new SqlParameter("@OpenQty", pos.OpeningPosition.CumQty));
                        //cmd.Parameters.Add(new SqlParameter("@OpenPosClOrdId", null));
                        //cmd.Parameters.Add(new SqlParameter("@ClosingPosClOrId", null));

                    }
                    else {

                        cmd.Parameters.Add(new SqlParameter("@IsClosing", "Y"));
                        cmd.Parameters.Add(new SqlParameter("@ClosingPosId", pos.ClosingPosition.PosId));
                        cmd.Parameters.Add(new SqlParameter("@ClosingPosClOrId", pos.ClosingPosition.GetLastFilledClOrdId()));
                        cmd.Parameters.Add(new SqlParameter("@CloseQty", pos.ClosingPosition.CumQty));
                        //cmd.Parameters.Add(new SqlParameter("@OpenPosClOrdId", null));
                        //cmd.Parameters.Add(new SqlParameter("@ClosingPosClOrId", null));

                    }

                    cmd.ExecuteNonQuery();
                }
                connection.Dispose();
            }

        }


        #endregion
    }
}
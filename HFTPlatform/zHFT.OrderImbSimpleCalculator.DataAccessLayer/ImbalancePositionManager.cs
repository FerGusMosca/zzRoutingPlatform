using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.OrderImbSimpleCalculator.BusinessEntities;
using zHFT.OrderImbSimpleCalculator.Common.Configuration;

namespace zHFT.OrderImbSimpleCalculator.DataAccessLayer
{
    public class ImbalancePositionManager
    {
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        public Configuration Configuration { get; set; }

        #endregion

        #region Constructor

        public ImbalancePositionManager(string adoConnectionString,Configuration pConfiguration)
        {
            ADOConnectionString = adoConnectionString;
            Configuration = pConfiguration;
        }

        #endregion

        #region Private Methods

        private ImbalancePosition BuildImbalancePosition(SqlDataReader reader)
        {
            ImbalancePosition imbPos = new ImbalancePosition();

            imbPos.StrategyName = reader["strategy_name"].ToString();
            imbPos.OpeningDate = Convert.ToDateTime(reader["opening_date"]);
            imbPos.ClosingDate = reader["closing_date"] != DBNull.Value ? (DateTime?) Convert.ToDateTime(reader["closing_date"]) : null;
            imbPos.FeeTypePerTrade = Configuration.FeeTypePerTrade;
            imbPos.FeeValuePerTrade = Configuration.FeeValuePerTrade;
            //OBS: No vamos a cargar el Opening Imbalance ya que estimamos que en un principio puede no llegar a ser necesario

            Position openingPos = new Position();
            openingPos.Security = new Security()
            {
                Symbol = reader["symbol"].ToString(),
                Currency = Configuration.Currency,
                SecType = Security.GetSecurityType(Configuration.SecurityTypes)
            };
            openingPos.Side = reader["trade_direction"].ToString() == ImbalancePosition._LONG ? Side.Buy : Side.Sell;
            openingPos.PriceType = PriceType.FixedAmount;
            openingPos.NewPosition = true;
            openingPos.CashQty = Configuration.PositionSizeInCash;
            openingPos.Qty = Convert.ToDouble(reader["qty"]);
            openingPos.QuantityType = QuantityType.CURRENCY;
            openingPos.CumQty = Convert.ToDouble(reader["qty"]);
            openingPos.AvgPx = Convert.ToDouble(reader["opening_price"]);
            openingPos.LeavesQty = 0;
            openingPos.PosStatus = zHFT.Main.Common.Enums.PositionStatus.Filled;
            openingPos.StopLossPct = Convert.ToDouble(Configuration.StopLossForOpenPositionPct);
            openingPos.PositionCanceledOrRejected = false;
            openingPos.PositionCleared = true;
            openingPos.AccountId = "TEST";


            imbPos.OpeningPosition = openingPos;

            return imbPos; 
        }

        #endregion


        #region Public Methods

        public List<ImbalancePosition> GetImbalancePositions(string strategyName, bool onlyActive)
        {
            using (var connection = new SqlConnection(ADOConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("GetSimpleImbalanceTrades", connection);

                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter param1 = cmd.Parameters.Add("@StrategyName", SqlDbType.VarChar);
                param1.Direction = ParameterDirection.Input;
                param1.Value = strategyName;


                SqlParameter param2 = cmd.Parameters.Add("@OnlyActive", SqlDbType.Bit);
                param2.Direction = ParameterDirection.Input;
                param2.Value = onlyActive;


                SqlDataReader reader = null;
                List<ImbalancePosition> imbPositions = new List<ImbalancePosition>();
                try
                {
                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        imbPositions.Add(BuildImbalancePosition(reader));
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

                return imbPositions;
            }
            
        }


        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.OrderImbSimpleCalculator.BusinessEntities;
using zHFT.Main.Common.Enums;
using zHFT.Main.DataAccessLayer.Managers.ADO;

namespace zHFT.OrderImbSimpleCalculator.DataAccessLayer
{
    public class CustomImbalanceConfigManager: ADOBaseManager
    {

        #region Constructor

        public CustomImbalanceConfigManager(string adoConnectionString)
        {
            ConnectionString = adoConnectionString;
            
        }

        #endregion


        #region Private Methods

        private CustomImbalanceConfig BuildCustomImbalanceConfig(SqlDataReader reader)
        {
            CustomImbalanceConfig conf = new CustomImbalanceConfig();

            conf.Symbol = reader["symbol"].ToString();
            conf.OpenImbalance = GetSafeDecimal(reader, "open_imbalance");
            conf.CloseImbalance = GetSafeDecimal(reader, "close_imbalance");
            conf.CloseWindow = GetNullSafeInt(reader, "close_window");
            conf.CloseTurtles = GetSafeBoolean(reader, "close_turtles");
            conf.CloseTurtles = GetSafeBoolean(reader, "close_turtles");
            conf.CloseMMov = GetSafeBoolean(reader, "close_mmov");
            conf.CloseOnImbalance = GetSafeBoolean(reader, "close_on_imbalance");


            return conf;
        }

        #endregion

        #region Public Methods

        public CustomImbalanceConfig GetCustomImbalanceConfigs(string symbol)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("GetCustomImbalanceConfig", connection);

                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter param1 = cmd.Parameters.Add("@Symbol", SqlDbType.VarChar);
                param1.Direction = ParameterDirection.Input;
                param1.Value = symbol;


                SqlDataReader reader = null;
                CustomImbalanceConfig  config = null;
                try
                {
                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        config=BuildCustomImbalanceConfig(reader);
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

                return config;
            }

        }


        #endregion
    }
}

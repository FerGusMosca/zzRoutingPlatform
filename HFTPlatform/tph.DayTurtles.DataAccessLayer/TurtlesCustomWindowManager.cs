using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;
using zHFT.Main.DataAccessLayer.Managers.ADO;

namespace tph.DayTurtles.DataAccessLayer
{
    public class TurtlesCustomWindowManager: ADOBaseManager
    {

        //

        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion

        #region Constructor

        public TurtlesCustomWindowManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion


        public List<TurtlesCustomConfig> GetTurtlesCustomWindow()
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand("GetCustomTurtleWindows", new SqlConnection(ADOConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;


            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            List < TurtlesCustomConfig > windowList= new   List<TurtlesCustomConfig>(); 

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {

                        TurtlesCustomConfig window = new TurtlesCustomConfig()
                        {
                            Symbol = reader["symbol"].ToString(),
                            OpenWindow = Convert.ToInt32(reader["open_window"]),
                            CloseWindow = Convert.ToInt32(reader["close_window"]),
                            TakeProfitPct = GetNullableDecimal(reader, "take_profit_pct"),
                            ExitOnMMov = GetSafeBoolean(reader,"exit_on_mmov"),
                            ExitOnTurtles = GetSafeBoolean(reader, "exit_on_turtles"),

                        };
                        windowList.Add(window);
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return windowList;
        }


    }
}

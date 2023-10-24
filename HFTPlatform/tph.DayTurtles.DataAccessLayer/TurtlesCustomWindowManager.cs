using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tph.DayTurtles.BusinessEntities;

namespace tph.DayTurtles.DataAccessLayer
{
    public class TurtlesCustomWindowManager
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


        public List<TurtlesCustomWindow> GetTurtlesCustomWindow()
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand("GetCustomTurtleWindows", new SqlConnection(ADOConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;


            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;
            List < TurtlesCustomWindow > windowList= new   List<TurtlesCustomWindow>(); 

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {

                        TurtlesCustomWindow window = new TurtlesCustomWindow()
                        {
                            Symbol = reader["symbol"].ToString(),
                            OpenWindow = Convert.ToInt32(reader["open_window"]),
                            CloseWindow = Convert.ToInt32(reader["close_window"]),

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

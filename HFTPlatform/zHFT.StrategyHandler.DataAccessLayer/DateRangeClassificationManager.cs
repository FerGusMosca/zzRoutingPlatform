using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.DataAccessLayer.Managers.ADO;
using zHFT.StrategyHandler.BusinessEntities;

namespace zHFT.StrategyHandler.DataAccessLayer
{
    public class DateRangeClassificationManager : ADOBaseManager
    {
        #region Protected Contss

        public static string _PERSIST_DATE_RANGE_CLASSIFICATION = "PersistDateRangeClassification";

        #endregion

        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion

        #region Constructor

        public DateRangeClassificationManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion


        #region Public Methods

        public void Persist(DateRangeClassification drClassif)
        {
            using (var connection = new SqlConnection(ADOConnectionString))
            {
                using (SqlCommand cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = _PERSIST_DATE_RANGE_CLASSIFICATION;
                    cmd.Parameters.Add(new SqlParameter("@Key", drClassif.Key));
                    cmd.Parameters.Add(new SqlParameter("@DateStart", drClassif.DateStart));
                    cmd.Parameters.Add(new SqlParameter("@DateEnd", drClassif.DateEnd));
                    cmd.Parameters.Add(new SqlParameter("@Classification", drClassif.Classification));

                    cmd.ExecuteNonQuery();
                }
                connection.Dispose();
            }

        }

        #endregion

    }
}

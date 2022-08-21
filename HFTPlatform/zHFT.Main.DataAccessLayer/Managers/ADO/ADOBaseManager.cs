using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace zHFT.Main.DataAccessLayer.Managers.ADO
{
    public class ADOBaseManager
    {
        #region Protected Attributes

        protected SqlConnection DatabaseConnection { get; set; }
        
        protected string ConnectionString { get; set; }


        #endregion

        #region Public Methods
        
        public static bool HasColumn(DbDataReader Reader, string ColumnName) { 
            foreach (DataRow row in Reader.GetSchemaTable().Rows) { 
                if (row["ColumnName"].ToString() == ColumnName) 
                    return true; 
            } //Still here? Column not found. 
            return false; 
        }

        public void Dispose()
        {
            DatabaseConnection.Dispose();
        }

        #endregion
        
        #region Protected Methods

       
        #endregion
    }
}

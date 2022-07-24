using System;
using System.Collections.Generic;
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

        public void Dispose()
        {
            DatabaseConnection.Dispose();
        }

        #endregion
        
        #region Protected Methods

       
        #endregion
    }
}

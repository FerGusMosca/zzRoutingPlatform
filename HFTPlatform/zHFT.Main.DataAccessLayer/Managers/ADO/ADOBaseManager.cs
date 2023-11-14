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


        protected string GetSafeString(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return Convert.ToString(rdr[ordinal]);
            else

                return null;

        }

        protected DateTime? GetSafeDateTime(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return (DateTime?) Convert.ToDateTime(rdr[ordinal]);
            else

                return null;

        }

        protected char? GetSafeChar(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return (char?) Convert.ToChar(rdr[ordinal]);
            else

                return null;

        }


        protected double? GetNullSafeDouble(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return (double?) Convert.ToDouble(rdr[ordinal]);
            else

                return null;

        }

        protected int? GetNullSafeInt(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return (int?) Convert.ToInt32(rdr[ordinal]);
            else

                return null;

        }



        protected long? GetNullSafeLong(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return Convert.ToInt64(rdr[ordinal]);
            else

                return null;

        }


        protected double GetSafeDouble(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return Convert.ToDouble(rdr[ordinal]);
            else

                return 0;

        }

        protected decimal GetSafeDecimal(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return Convert.ToDecimal(rdr[ordinal]);
            else

                return 0;

        }


        protected bool GetSafeBoolean(SqlDataReader rdr, string col)
        {
            var ordinal = rdr.GetOrdinal(col);
            if (!rdr.IsDBNull(ordinal))
                return Convert.ToBoolean(rdr[ordinal]);
            else

                return false;

        }

        #endregion
    }
}

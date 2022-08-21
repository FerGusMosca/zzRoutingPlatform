using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;

namespace zHFT.Main.DataAccessLayer.Managers.ADO
{
    public class ADOExecutionSummaryManager : ADOBaseManager
    {
        #region Constructors

        public ADOExecutionSummaryManager(string pConnectionString)
        {
            ConnectionString = pConnectionString;
        }

        #endregion

        #region Private Static Querys

        private static string _SP_PERSIST_EXECUTION_SUMMARY = "PersistExecutionSummary";
        
        private static string _SP_PERSIST_POSITION= "PersistPosition";
        
        private static string _SP_PERSIST_ORDER= "PersistOrder";
        
        private static string _SP_PERSIST_EXECUTION_REPORT= "PersistExecutionReport";

        #endregion

        #region Private Methods

        public long PersistExecutionSummary(ExecutionSummary summary)
        {
            //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_PERSIST_EXECUTION_SUMMARY, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", summary.Id);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Date", summary.Date);
            cmd.Parameters["@Date"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Symbol", summary.Symbol);
            cmd.Parameters["@Symbol"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@AvgPx", summary.AvgPx);
            cmd.Parameters["@AvgPx"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@CumQty", summary.CumQty);
            cmd.Parameters["@CumQty"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@LeavesQty", summary.LeavesQty);
            cmd.Parameters["@LeavesQty"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Commission", summary.Commission);
            cmd.Parameters["@Commission"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Text", summary.Text);
            cmd.Parameters["@Text"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PosId", summary.Position != null ? (int?) summary.Position.Id : null);
            cmd.Parameters["@PosId"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Console", summary.Console);
            cmd.Parameters["@Console"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@AccountNumber", summary.AccountNumber);
            cmd.Parameters["@AccountNumber"].Direction = ParameterDirection.Input;

            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        summary.Id = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return summary.Id;

        }

        public long PersistOrder(Order order, long posId)
        {
            
             //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_PERSIST_ORDER, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", order.Id);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@ClOrdId", order.ClOrdId);
            cmd.Parameters["@ClOrdId"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@OrderId", order.OrderId);
            cmd.Parameters["@OrderId"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Symbol", order.Symbol);
            cmd.Parameters["@Symbol"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@SettlType", Convert.ToChar(order.SettlType).ToString());
            cmd.Parameters["@SettlType"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@SettlDate", order.SettlDate);
            cmd.Parameters["@SettlDate"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Side",Convert.ToChar(order.Side).ToString());
            cmd.Parameters["@Side"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Exchange", order.Exchange);
            cmd.Parameters["@Exchange"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@OrdType", Convert.ToChar(order.OrdType).ToString());
            cmd.Parameters["@OrdType"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@QuantityType", Convert.ToInt32(order.QuantityType));
            cmd.Parameters["@QuantityType"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@OrdQty", order.OrderQty);
            cmd.Parameters["@OrdQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@CashOrderQty", order.CashOrderQty);
            cmd.Parameters["@CashOrderQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@OrderPercent", order.OrderPercent);
            cmd.Parameters["@OrderPercent"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@PriceType", Convert.ToInt32(order.PriceType));
            cmd.Parameters["@PriceType"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Price", order.Price);
            cmd.Parameters["@Price"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@StopPx", order.StopPx);
            cmd.Parameters["@StopPx"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Currency", order.Currency);
            cmd.Parameters["@Currency"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@TimeInForce", order.TimeInForce);
            cmd.Parameters["@TimeInForce"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@ExpireTime", order.ExpireTime);
            cmd.Parameters["@ExpireTime"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@EffectiveTime", order.EffectiveTime);
            cmd.Parameters["@EffectiveTime"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@MinQty", order.MinQty);
            cmd.Parameters["@MinQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Index", order.Index);
            cmd.Parameters["@Index"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@PosId", posId);
            cmd.Parameters["@PosId"].Direction = ParameterDirection.Input;
            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        order.Id = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return order.Id;

            
        }

        public int PersistPosition(Position pos)
        {
             //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_PERSIST_POSITION, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", pos.Id);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PosId", pos.Id);
            cmd.Parameters["@PosId"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Symbol", pos.Symbol);
            cmd.Parameters["@Symbol"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PosStatus",Convert.ToChar(pos.PosStatus).ToString());
            cmd.Parameters["@PosStatus"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Side",Convert.ToChar(pos.Side).ToString());
            cmd.Parameters["@Side"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Exchange", pos.Exchange);
            cmd.Parameters["@Exchange"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@QuantityType", Convert.ToInt32(pos.QuantityType));
            cmd.Parameters["@QuantityType"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@PriceType", Convert.ToInt32(pos.PriceType));
            cmd.Parameters["@PriceType"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@Qty", pos.Qty);
            cmd.Parameters["@Qty"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@CashQty", pos.CashQty);
            cmd.Parameters["@CashQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Percent", pos.Percent);
            cmd.Parameters["@Percent"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@CumQty", pos.CumQty);
            cmd.Parameters["@CumQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@LeavesQty", pos.LeavesQty);
            cmd.Parameters["@LeavesQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@AvgPx", pos.AvgPx);
            cmd.Parameters["@AvgPx"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@LastQty", pos.LastQty);
            cmd.Parameters["@LastQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@LastMkt", pos.LastMkt);
            cmd.Parameters["@LastMkt"].Direction = ParameterDirection.Input;
            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        pos.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return pos.Id;
            
            
        }

        public long PersistExecutionReport(ExecutionReport execRep,long posId)
        {
             //DatabaseConnection = new MySqlConnection(ConnectionString);
            SqlCommand cmd = new SqlCommand(_SP_PERSIST_EXECUTION_REPORT, new SqlConnection(ConnectionString));
            cmd.CommandTimeout = 60;

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Id", execRep.Id);
            cmd.Parameters["@Id"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@ExecId", execRep.ExecID);
            cmd.Parameters["@ExecId"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@TransactTime", execRep.TransactTime);
            cmd.Parameters["@TransactTime"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@ExecType", Convert.ToChar(execRep.ExecType).ToString());
            cmd.Parameters["@ExecType"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@OrdStatus", Convert.ToChar(execRep.OrdStatus).ToString());
            cmd.Parameters["@OrdStatus"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@OrdRejReason", execRep.OrdRejReason != null ? (int?)Convert.ToInt32(execRep.OrdRejReason) : null);
            cmd.Parameters["@OrdRejReason"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@LastQty", execRep.LastQty);
            cmd.Parameters["@LastQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@LastPx", execRep.LastPx);
            cmd.Parameters["@LastPx"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@LastMkt", execRep.LastMkt);
            cmd.Parameters["@LastMkt"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@LeavesQty", execRep.LeavesQty);
            cmd.Parameters["@LeavesQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@CumQty", execRep.CumQty);
            cmd.Parameters["@CumQty"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@AvgPx", execRep.AvgPx);
            cmd.Parameters["@AvgPx"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Commission", execRep.Commission);
            cmd.Parameters["@Commission"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@Text", execRep.Text);
            cmd.Parameters["@Text"].Direction = ParameterDirection.Input;
            
            cmd.Parameters.AddWithValue("@PosId", posId);
            cmd.Parameters["@PosId"].Direction = ParameterDirection.Input;
      
            
            cmd.Connection.Open();

            // Open DB
            SqlDataReader reader;

            try
            {
                // Run Query
                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        execRep.Id = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                }
            }
            finally
            {
                cmd.Connection.Close();
            }

            return execRep.Id;
        }

        #endregion
        
        #region Public Methods

        public void Insert(ExecutionSummary summary)
        {
            
            //TransactionScope
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
            {
                summary.Position.Id = PersistPosition(summary.Position);

                summary.Id = PersistExecutionSummary(summary);

                foreach (Order order in summary.Position.Orders.Where(x => Order.ActiveStatus(x.OrdStatus)))
                {
                    order.Id = PersistOrder(order, summary.Position.Id);
                }

                foreach (ExecutionReport execRep in summary.Position.ExecutionReports.Where(x => x.IsActiveOrder()))
                {

                    execRep.Id = PersistExecutionReport(execRep, summary.Position.Id);
                }
                
                scope.Complete();
            }
        }


        #endregion
    }
}
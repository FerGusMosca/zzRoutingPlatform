using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.OrderImbSimpleCalculator.BusinessEntities;

namespace zHFT.OrderImbSimpleCalculator.DataAccessLayer
{
    public class SecurityImbalanceManager
    {
        #region Protected Attributes

        public string ADOConnectionString { get; set; }

        #endregion

        #region Constructor

        public SecurityImbalanceManager(string adoConnectionString)
        {
            ADOConnectionString = adoConnectionString;
        }

        #endregion

        #region Public Methods

        public void PersistSecurityImbalance(SecurityImbalance secImbal)
        {
            using (var connection = new SqlConnection(ADOConnectionString))
            {
                using (SqlCommand cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "PersistSimpleImbalance";
                    cmd.Parameters.Add(new SqlParameter("@Symbol", secImbal.Security.Symbol));
                    cmd.Parameters.Add(new SqlParameter("@DateTime", secImbal.DateTime));
                    cmd.Parameters.Add(new SqlParameter("@TradesOnBid", secImbal.CountTradeOnBid));
                    cmd.Parameters.Add(new SqlParameter("@CumSizeOnBid", secImbal.SizeTradeOnBid));
                    cmd.Parameters.Add(new SqlParameter("@TradesOnAsk", secImbal.CountTradeOnAsk));
                    cmd.Parameters.Add(new SqlParameter("@CumSizeOnAsk", secImbal.SizeTradeOnAsk));
                    cmd.Parameters.Add(new SqlParameter("@TradesOnBidImbalance", secImbal.BidCountImbalance));
                    cmd.Parameters.Add(new SqlParameter("@CumSizeOnBidImbalance", secImbal.BidSizeImbalance));
                    cmd.Parameters.Add(new SqlParameter("@TradesOnAskImbalance", secImbal.AskCountImbalance));
                    cmd.Parameters.Add(new SqlParameter("@CumSizeOnAskImbalance", secImbal.AskSizeImbalance));

                    cmd.ExecuteNonQuery();
                }
                connection.Dispose();
            }
        
        }


        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.Bitmex.BusinessEntities
{
    public class ExecutionReport
    {
        #region Public Attribute

        public static string _ORD_STATUS_FILLED = "Filled";

        public static string _ORD_STATUS_PARTIALLY_FILLED = "PartiallyFilled";

        #endregion

        #region Public Attributes
        public string ExecID { get; set; }

        public string OrderID { get; set; }

        public string ClOrdID { get; set; }

        public decimal? Account { get; set; }

        public string Symbol { get; set; }

        public string Side { get; set; }

        public double? LastQty { get; set; }

        public double? LastPx { get; set; }

        public double? UnderlyingLastPx { get; set; }

        public decimal? OrderQty { get; set; }

        public double? Price { get; set; }

        public double? StopPx { get; set; }

        public string Currency { get; set; }

        public string OrdType { get; set; }

        public string ExecType { get; set; }

        public string TimeInForce { get; set; }

        public string OrdStatus { get; set; }

        public string OrdRejReason { get; set; }

        public double? SimpleLeavesQty { get; set; }

        public decimal? LeavesQty { get; set; }

        public double? SimpleCumQty { get; set; }

        public double? CumQty { get; set; }

        public double? AvgPx { get; set; }

        public double? Commission { get; set; }

        public string Text { get; set; }

        public DateTime? TransactTime { get; set; }

        public DateTime? Timestamp { get; set; }

        public Order Order { get; set; }


        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.Binance.Common.DTO
{
    public class ExecutionReportDTO
    {
        #region Public Static Consts

        public static string _REJECTED = "REJECTED";

        public static string _CANCELED = "CANCELED";

        #endregion

        #region Public Attributes

        public Order Order { get; set; }

        public decimal ExecutedQty { get; set; }

        public decimal LeavesQty { get; set; }

        public decimal OrigQty { get; set; }

        public string Status { get; set; }

        public string Text { get; set; }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tph.OrderRouters.IB.Common.DTO
{
    public class OrderStatusDTO
    {
        #region Public Static Conts

        public static string _STATUS_PENDING_SUBMIT = "PendingSubmit";
        public static string _STATUS_PENDING_CANCEL = "PendingCancel";
        public static string _STATUS_PRE_SUBMITTED = "PreSubmitted";
        public static string _STATUS_SUBMITTED = "Submitted";
        public static string _STATUS_CANCELED = "Cancelled";
        public static string _STATUS_INACTIVE = "Inactive";
        //public static string _STATUS_FILLED = "Filled Filled";
        public static string _STATUS_FILLED = "Filled";
        //public static string _STATUS_PARTIALLY_FILLED = "Submitted Filled";

        public static string _STATUS_REJECTED = "REJECTED";

        private static int _CANCELLED_ERROR = 202;

        #endregion

        #region Public Attributes

        public int Id { get; set; }

        public string Status { get; set; }

        public double Filled { get; set; }

        public double Remaining { get; set; }

        public double AvgFillPrice { get; set; }

        public int PermId { get; set; }

        public int ParentId { get; set; }

        public double LastFillPrice { get; set; }

        public int CliendId { get; set; }

        public string WhyHeld { get; set; }

        public int ErrorCode { get; set; }

        public string ErrorMsg { get; set; }

        public string ClOrdId { get; set; }

        #endregion

        #region Public Static Methods

        public static string GetStatusByErrorCode(int errorCode)
        {
            //Until we have more detail alll the error which we don't process will be a _STATUS_REJECTED
            if (errorCode == _CANCELLED_ERROR)
                return _STATUS_CANCELED;
            else
                return _STATUS_REJECTED;
        
        }

        #endregion
    }
}

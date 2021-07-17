using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using zHFT.Main.Common.Enums;

namespace zHFT.Main.BusinessEntities.Orders
{
    public class ExecutionReport
    {
        #region Public Static Consts

        public static string _DATE_FORMAT = "%d-%m-%Y %H:%M:%S";
        
        #endregion
        
        #region Public Attributes

        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy hh:mm:ss")]
        public DateTime? TransactTime { get; set; }

        public long Id { get; set; }

        public string ExecID { get; set; }

        public ExecType ExecType { get; set; }

        public OrdStatus OrdStatus { get; set; }

        public OrdRejReason? OrdRejReason { get; set; }

        public Order Order { get; set; }

        public double? LastQty { get; set; }

        public double? LastPx { get; set; }

        public string  LastMkt { get; set; }

        public double LeavesQty { get; set; }

        public double CumQty { get; set; }

        public double? AvgPx { get; set; }

        public double? Commission { get; set; }

        public string Text { get; set; }

        #endregion


        #region Constructors

        public ExecutionReport() 
        {
            Order = new Order();
        
        }
        #endregion

        #region Public Methods

        public bool IsCancelationExecutionReport()
        {
            return ExecType == ExecType.DoneForDay || ExecType == ExecType.Stopped
                                     || ExecType == ExecType.Suspended || ExecType == ExecType.Rejected
                                     || ExecType == ExecType.Expired || ExecType == ExecType.Canceled;
        
        }

        public string GetRejectReason()
        {
            if (IsCancelationExecutionReport())
            {
                string reason = "";

                if (OrdRejReason != null)
                    reason = OrdRejReason.Value.ToString() + "-" + Text;
                else
                    reason = Text;

                return reason;
            }
            else
                return "-";
        }

        #endregion
    }

    public class DateFormatConverter : IsoDateTimeConverter
    {
        public DateFormatConverter(string format)
        {
            DateTimeFormat = format;
        }
    }
}

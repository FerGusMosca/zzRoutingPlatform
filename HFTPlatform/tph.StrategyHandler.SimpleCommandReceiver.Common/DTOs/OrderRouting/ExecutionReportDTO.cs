using System;
using Newtonsoft.Json;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class ExecutionReportDTO:ExecutionReport
    {
        #region Constructors

        public ExecutionReportDTO(ExecutionReport er)
        {
            TransactTime = er.TransactTime;
            Id = er.Id;
            ExecID = er.ExecID;
            ExecType = er.ExecType;
            OrdStatus = er.OrdStatus;
            OrdRejReason = er.OrdRejReason;
            Order = er.Order;
            LastQty = er.LastQty;
            LastPx = er.LastPx;
            LastMkt = er.LastMkt;
            LeavesQty = er.LeavesQty;
            CumQty = er.CumQty;
            AvgPx = er.AvgPx;
            Commission = er.Commission;
            Text = er.Text;

            Status = GetStrStatus();
            OrderId = Order != null ? Order.OrderId : null;
            ClOrdId = Order != null ? Order.ClOrdId : null;
            OrigClOrdId = Order != null ? Order.OrigClOrdId : null;

            if (ExecType == ExecType.Trade)
                LastFilledTime = DateTime.Now;

            if (Order != null)
                CreateTime = Order.EffectiveTime;

            DatetimeFormat = _DATE_FORMAT;
        }

        #endregion
        
        #region Public Static Consts

        public static string _PENDING_NEW = "PendingNew";
            
        public static string _NEW = "New";
        
        public static string _CANCELED = "Canceled";
        
        public static string _REJECTED = "Rejected";
        
        public static string _PARTIALLY_FILLED = "PartiallyFilled";
        
        public static string _FILLED = "Filled";
        
        public static string _EXPIRED = "Expired";
        
        public static string _UNKNWOWN = "Unknown";
        
        
        #endregion
        
        #region Public Attributes 
        
        public string Msg = "ExecutionReportMsg";
        
        public string Status { get; set; }
        
        public string OrderId { get; set; }
        
        public string ClOrdId { get; set; }
        
        public string OrigClOrdId { get; set; }
        
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy hh:mm:ss")]
        public DateTime? LastFilledTime { get; set; }
        
        [JsonConverter(typeof(DateFormatConverter), "dd-MM-yyyy hh:mm:ss")]
        public DateTime? CreateTime { get; set; }
        
        public string DatetimeFormat { get; set; }
        
        #endregion
        
        #region Public Methods

        public  string GetStrStatus()
        {
            if (OrdStatus == OrdStatus.PendingNew)
                return _PENDING_NEW;
            else if (OrdStatus == OrdStatus.New)
                return _NEW;
            else if (OrdStatus == OrdStatus.Canceled)
                return _CANCELED;
            else if (OrdStatus == OrdStatus.Rejected)
                return _REJECTED;
            else if (OrdStatus == OrdStatus.PartiallyFilled)
                return _PARTIALLY_FILLED;
            else if (OrdStatus == OrdStatus.Filled)
                return _FILLED;
            else if (OrdStatus == OrdStatus.Expired)
                return _EXPIRED;
            else
                return _UNKNWOWN;
            

        }

        #endregion
    }
}
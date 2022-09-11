using System;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;

namespace OrderBookLoaderMock.Common.DTO.Orders
{
    public class ExecutionReportMsg: ExecutionReport
    {
        #region Protected Consts

        public static string _SUBSCRIPTION_SEPARATOR = "#";
        
        
        #endregion
        
        #region Public Attributes 

        public string Msg = "ExecutionReportMsg";
        
        public string Status { get; set; }
        
        public string ClOrdId { get; set; }
        
        public string OrigClOrdId { get; set; }
        
        public string UUID { get; set; }
        
        public string Direction { get; set; }
        
        public bool ForceSubscription { get; set; }
        
        public string Login { get; set; }
        
        public long OrderCreationTime { get; set; }
        
        #endregion
        
        #region Public Methods
        
        protected static long GetEpochMilisec(DateTime? date)
        {
            if (date.HasValue)
            {
                TimeSpan span = date.Value - new DateTime(1970, 1, 1);

                return Convert.ToInt64(span.TotalMilliseconds);
            }
            else
            {
                return 0;
            }
        }

//        public void PopulateFromBase(CLOBOrder order)
//        {
//            TransactTime = order.LastExecutionReport.TransactTime;
//            Id = order.LastExecutionReport.Id;
//            ExecID = order.LastExecutionReport.ExecID;
//            ExecType = order.LastExecutionReport.ExecType;
//            OrdStatus = order.LastExecutionReport.OrdStatus;
//            OrdRejReason = order.LastExecutionReport.OrdRejReason;
//            Login = order.Shareholder != null && order.Shareholder.User != null ? order.Shareholder.User.Login : "?";
//            OrderCreationTime = GetEpochMilisec(order.TransactTime);
//
//
//            Order = new Order()
//            {
//                Security = new Security() {Symbol = order.Security.Symbol},
//                OrderQty = order.OrderQty,
//                Price = order.Price,
//                TimeInForce = order.TimeInForce,
//                OrdType = order.OrdType,
//                Exchange = order.Exchange,
//                Currency = order.Currency,
//                Side = order.Side,
//                Account = order.Account,
//                ClOrdId = order.ClOrdId,
//                OrigClOrdId = order.OrigClOrdId,
//                RejReason = order.RejReason,
//                Symbol = order.Symbol,
//                OrderId = order.OrderId
//            };
//            
//            LastQty = order.LastExecutionReport.LastQty;
//            LastPx = order.LastExecutionReport.LastPx;
//            LastMkt = order.LastExecutionReport.LastMkt;
//            LeavesQty = order.LastExecutionReport.LeavesQty;
//            CumQty = order.LastExecutionReport.CumQty;
//            AvgPx = order.LastExecutionReport.AvgPx;
//            Commission = order.LastExecutionReport.Commission;
//            Text = order.LastExecutionReport.Text;
//
//            ClOrdId = Order != null ? Order.ClOrdId : "";
//            OrigClOrdId = Order != null ? Order.OrigClOrdId : "";
//            Status = OrdStatus.ToString();
//            
//            if (order.ForcePropagation)
//            {
//                ForceSubscription = true;
//                order.ForcePropagation = false;
//            }
//            else
//            {
//                ForceSubscription = false;
//            }
//
//        }


        public override string ToString()
        {
            return String.Format("Execution Report-> Symbol={0} ClOrdId={1} OrigClOrdId={2} OrderId={3} " +
                                 "Status={4} ExecType={5} OrderPrice={9} CumQty={6} LvsQty={7} AvgPx={8}",
                                Order.Symbol, ClOrdId, OrigClOrdId,
                                Order.OrderId, OrdStatus, ExecType,
                                CumQty, LeavesQty, AvgPx,
                                Order.Price.HasValue ? Order.Price.Value.ToString() : "?");
        }

        public string GetSubscriptionSymbolKey()
        {
            string key = string.Format("{0}{1}{2}", Login, _SUBSCRIPTION_SEPARATOR, Order.Security.Symbol);
            return key;
        }

        #endregion
    }
}
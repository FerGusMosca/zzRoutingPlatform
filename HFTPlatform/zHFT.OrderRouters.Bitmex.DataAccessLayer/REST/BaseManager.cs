using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.OrderRouters.Bitmex.BusinessEntities;

namespace zHFT.OrderRouters.Bitmex.DataAccessLayer
{
    public class BaseManager
    {
        #region Protected Consts

        protected static string _URL_POSTFIX = "/api/v1";

        #endregion

        #region Protected Methods

        protected ExecutionReport MapExecutionReport(ExecutionReport execReport)
        {
            ExecutionReport beExecutionReport = new BusinessEntities.ExecutionReport();

            beExecutionReport.ExecID = execReport.ExecID;
            beExecutionReport.OrderID = execReport.OrderID;
            beExecutionReport.ClOrdID = execReport.ClOrdID;
            beExecutionReport.Account = execReport.Account;
            beExecutionReport.Symbol = execReport.Symbol;
            beExecutionReport.Side = execReport.Side;
            beExecutionReport.LastQty = execReport.LastQty;
            beExecutionReport.LastPx = execReport.LastPx;
            beExecutionReport.UnderlyingLastPx = execReport.UnderlyingLastPx;
            beExecutionReport.OrderQty = execReport.OrderQty;
            beExecutionReport.Price = execReport.Price;
            beExecutionReport.Currency = execReport.Currency;
            beExecutionReport.OrdType = execReport.OrdType;
            beExecutionReport.ExecType = execReport.ExecType;
            beExecutionReport.TimeInForce = execReport.TimeInForce;
            beExecutionReport.OrdStatus = execReport.OrdStatus;
            beExecutionReport.OrdRejReason = execReport.OrdRejReason;
            beExecutionReport.SimpleLeavesQty = execReport.SimpleLeavesQty;
            beExecutionReport.LeavesQty = execReport.LeavesQty;
            beExecutionReport.SimpleCumQty = execReport.SimpleCumQty;
            beExecutionReport.CumQty = execReport.CumQty;
            beExecutionReport.AvgPx = execReport.AvgPx;
            beExecutionReport.Commission = execReport.Commission;
            beExecutionReport.Text = execReport.Text;
            beExecutionReport.TransactTime = execReport.TransactTime;
            beExecutionReport.Timestamp = execReport.Timestamp;

            return beExecutionReport;

        }

        #endregion
    }
}

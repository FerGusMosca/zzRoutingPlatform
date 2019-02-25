using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.OrderRouters.Bitmex.BusinessEntities;
using zHFT.OrderRouters.Bitmex.DataAccessLayer.API;

namespace zHFT.OrderRouters.Bitmex.DataAccessLayer
{
    public class OrderManager : BaseManager
    {
        #region Protected Attributes

        public string URL { get; set; }

        protected string ID { get; set; }

        protected string Secret { get; set; }

        #endregion

        #region Constructors

        public OrderManager(string url, string pID, string pSecret)
        {
            URL = url;

            ID = pID;

            Secret = pSecret;

        }

        #endregion

        #region Public Methods

        public ExecutionReport PlaceOrder(Order order)
        {

            order.ValidateOrder();
            //We use the class that was provided by BitMex @https://github.com/BitMEX/api-connectors
            //Propper version for C++
            BitMEXApi api = new BitMEXApi(URL, ID, Secret);

            var param = new Dictionary<string, string>();
            param["clOrdId"] = order.ClOrdId;
            param["symbol"] = order.SymbolPair;
            param["side"] = order.GetSide();
            param["orderQty"] = order.OrderQty.HasValue ? order.OrderQty.Value.ToString("0.########") : "0";
            param["ordType"] = order.GetOrdType();

            if (order.TimeInForce.HasValue)
                param["timeInForce"] = order.TimeInForce.Value.ToString();

            if (order.Price.HasValue)
                param["price"] = order.Price.Value.ToString("0.########");

            if (order.StopPx.HasValue)
                param["stopPx"] = order.StopPx.Value.ToString("0.########");

            if (order.TriggeringPrice.HasValue)//In BitMex we assign the triggering price using the "stopPx" field
                param["stopPx"] = order.TriggeringPrice.Value.ToString("0.########");

            param["execInst"] = order.ExecInst;

            string resp = api.Query("POST", "/order", param, true);

            if (resp.Contains("error"))
                throw new Exception(resp);

            ExecutionReport report = JsonConvert.DeserializeObject<ExecutionReport>(resp);

            ExecutionReport beExecReport = MapExecutionReport(report);

            order.OrderId = beExecReport.OrderID;
            beExecReport.Order = order;

            return beExecReport;

        }

        public ExecutionReport UpdateOrder(Order order)
        {
            //We use the class that was provided by BitMex @https://github.com/BitMEX/api-connectors
            //Propper version for C++
            BitMEXApi api = new BitMEXApi(URL, ID, Secret);

            var param = new Dictionary<string, string>();
            //param["clOrdID"] = order.ClOrdId;
            param["orderID"] = order.OrderId;
            param["orderQty"] = order.OrderQty.HasValue ? order.OrderQty.Value.ToString("0.########") : "0";
            if (order.Price.HasValue)
                param["price"] = order.Price.Value.ToString("0.########");

            string resp = api.Query("PUT", "/order", param, true);

            ExecutionReport report = JsonConvert.DeserializeObject<ExecutionReport>(resp);

            ExecutionReport beExecReport = MapExecutionReport(report);

            beExecReport.Order = order;

            return beExecReport;
        }

        public ExecutionReport CancelOrder(Order order)
        {
            BitMEXApi api = new BitMEXApi(URL, ID, Secret);
            ExecutionReport beExecReport = null;

            var param = new Dictionary<string, string>();
            //param["clOrdIDs"] = order.ClOrdId;
            param["orderID"] = order.OrderId;
            //param["orderID"] = "132";

            string resp = api.Delete("/order", param, true);

            ExecutionReport[] reports = JsonConvert.DeserializeObject<ExecutionReport[]>(resp);

            if (reports.Length > 0)
                beExecReport = MapExecutionReport(reports[0]);
            else
                throw new Exception(string.Format("No execution report received on order cancelation:{0}", order.OrderId));

            return beExecReport;
        }

        public ExecutionReport[] CancelAll()
        {
            BitMEXApi api = new BitMEXApi(URL, ID, Secret);
            List<ExecutionReport> beExecReports = new List<ExecutionReport>();

            var param = new Dictionary<string, string>();

            string resp = api.Delete("/order/all", param, true);

            ExecutionReport[] reports = JsonConvert.DeserializeObject<ExecutionReport[]>(resp);

            foreach (ExecutionReport report in reports)
                beExecReports.Add(MapExecutionReport(report));

            return beExecReports.ToArray();
        }

        #endregion
    }
}

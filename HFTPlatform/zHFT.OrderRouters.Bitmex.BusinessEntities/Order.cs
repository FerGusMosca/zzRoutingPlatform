using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.Bitmex.BusinessEntities
{
    public class Order
    {
        #region Public Attributes

        #region Constructors

        public Order()
        {

            ExecutionReports = new List<ExecutionReport>();
            LastPxList = new List<double>();
            LastSharesList = new List<double>();


        }

        #endregion

        #region Private Static Consts

        private static string _BUY = "Buy";
        private static string _SELL = "Sell";

        private static string _ORD_TYPE_MARKET = "Market";
        private static string _ORD_TYPE_LIMIT = "Limit";
        private static string _ORD_TYPE_STOP_LIMIT = "StopLimit";
        private static string _ORD_TYPE_STOP = "Stop";
        private static string _ORD_MARKET_IF_TOUCHED = "MarketIfTouched";
        private static string _ORD_LIMIT_IF_TOUCHED = "LimitIfTouched";
        private static string _ORD_MARKET_WITH_LEFTOVER_AS_LIMIT = "MarketWithLeftOverAsLimit";

        private static string _EXEC_INST_MARK_PRICE = "MarkPrice";

        private static string _TIF_GTC = "GoodTillCancel";

        #endregion

        #region Public Attributes

        public long PriceLevelId { get; set; }

        public string OrderId { get; set; }

        public string ClOrdId { get; set; }

        public string PendingClOrdId { get; set; }

        public string SymbolPair { get; set; }

        public Side Side { get; set; }

        public string Currency { get; set; }

        private OrdType _ordType;
        public OrdType OrdType
        {
            get
            {
                return _ordType;
            }

            set
            {
                _ordType = value;

            }
        }

        public string ExecInst { get; set; }

        public decimal? OrderQty { get; set; }

        public decimal? Size { get; set; }

        public decimal? Price { get; set; }

        public decimal? StopPx { get; set; }

        public decimal? TriggeringPrice { get; set; }

        public TimeInForce? TimeInForce { get; set; }

        #endregion

        #region Executions

        public List<ExecutionReport> ExecutionReports { get; set; }

        public List<double> LastPxList { get; set; }

        public List<double> LastSharesList { get; set; }

        #endregion

        #endregion

        #region LastPx and LastShares Calculation

        private double CalculateSum1()
        {
            double sum1 = 0;

            for (int i = 0; i < LastPxList.Count; i++)
                sum1 += LastPxList[i] * LastSharesList[i];

            return sum1;
        }

        private double CalculateSumPrevLastPx()
        {
            double sum2 = 0;

            for (int i = 0; i < LastPxList.Count; i++)
                sum2 += LastPxList[i];

            return sum2;
        }

        //We get the total shares traded before the current execution report
        //Useful to calculate the LastShares for current Exec Report ==> ExecReport.LastShares = ExecReport.CumQty - Sum2
        private double CalculateSumPrevCumQty()
        {
            double sum2 = 0;

            for (int i = 0; i < LastSharesList.Count; i++)
                sum2 += LastSharesList[i];

            return sum2;
        }

        private double Get1e8()
        {
            return Math.Pow(10, 8);
        }

        private double CalculateSum1Satoshi()
        {
            double sum1 = 0;
            double d1e8 = Get1e8();

            for (int i = 0; i < LastPxList.Count; i++)
            {
                double lastPxi = LastPxList[i];
                double lastSharesi = LastSharesList[i];
                sum1 += (d1e8 / lastPxi) * lastSharesi;
            }

            return sum1;
        }

        public void CalculateLastSharesAndLastPxOnSatoshiAverages(ExecutionReport lastExecReport)
        {
            //we cannot trust OrdStatusFilled
            //if (lastExecReport.OrdStatus == ExecutionReport._ORD_STATUS_FILLED ||
            //    lastExecReport.OrdStatus == ExecutionReport._ORD_STATUS_PARTIALLY_FILLED)
            if (lastExecReport.AvgPx.HasValue)
            {
                string orderId = lastExecReport.OrderID;//Just with testing purposes
                double sum1 = CalculateSum1Satoshi();
                double prevCumQty = CalculateSumPrevCumQty();
                double avgPx = lastExecReport.AvgPx.Value;
                double d1e8 = Get1e8();

                //Proper validations to not have null values should be made
                double lastShares = lastExecReport.CumQty.Value - prevCumQty;

                double lastPx = (d1e8 * lastShares) / (((d1e8 * lastExecReport.CumQty.Value) / avgPx) - sum1);

                LastSharesList.Add(lastShares);
                LastPxList.Add(lastPx);

                lastExecReport.LastQty = lastShares;
                lastExecReport.LastPx = lastPx;
            }
        }

        ////We make the calculations and save the calculated values in the order lists
        public void CalculateLastSharesAndLastPxOnPonderatedAverages(ExecutionReport lastExecReport)
        {
            //we cannot trust OrdStatusFilled
            //if (lastExecReport.OrdStatus == ExecutionReport._ORD_STATUS_FILLED ||
            //    lastExecReport.OrdStatus == ExecutionReport._ORD_STATUS_PARTIALLY_FILLED)
            if (lastExecReport.AvgPx.HasValue)
            {
                string orderId = lastExecReport.OrderID;//Just with testing purposes
                double sum1 = CalculateSum1();
                double prevCumQty = CalculateSumPrevCumQty();

                //Proper validations to not have null values should be made
                double lastShares = lastExecReport.CumQty.Value - prevCumQty;
                double lastPx = ((lastExecReport.AvgPx.Value * lastExecReport.CumQty.Value) - sum1) / (lastExecReport.CumQty.Value - prevCumQty);

                LastSharesList.Add(lastShares);
                LastPxList.Add(lastPx);

                lastExecReport.LastQty = lastShares;
                lastExecReport.LastPx = lastPx;
            }
        }

        ////We make the calculations and save the calculated values in the order lists
        public void CalculateLastSharesAndLastPxOnSimpleAverages(ExecutionReport lastExecReport)
        {
            //we cannot trust OrdStatusFilled
            //if (lastExecReport.OrdStatus == ExecutionReport._ORD_STATUS_FILLED ||
            //    lastExecReport.OrdStatus == ExecutionReport._ORD_STATUS_PARTIALLY_FILLED)
            if (lastExecReport.AvgPx.HasValue)
            {
                string orderId = lastExecReport.OrderID;//Just with testing purposes
                double sumPrevLastPx = CalculateSumPrevLastPx();
                double prevCumQty = CalculateSumPrevCumQty();

                //Proper validations to not have null values should be made
                double lastShares = lastExecReport.CumQty.Value - prevCumQty;
                double lastPx = ((LastSharesList.Count + 1) * lastExecReport.AvgPx.Value) - sumPrevLastPx;

                LastSharesList.Add(lastShares);
                LastPxList.Add(lastPx);

                lastExecReport.LastQty = lastShares;
                lastExecReport.LastPx = lastPx;
            }
        }

        #endregion

        #region Public Methods


        public static Side GetSide(string side)
        {
            if (side == _BUY)
                return Side.Buy;
            else if (side == _SELL)
                return Side.Sell;
            else
                return Side.Unknown;
        }

        public string GetSide()
        {
            if (Side == Side.Buy)
                return _BUY;
            else if (Side == Side.Sell)
                return _SELL;
            else
                throw new Exception(string.Format("Invalid Side for order on pair {0}", SymbolPair));

        }

        public string GetOrdType()
        {
            if (OrdType == OrdType.Limit)
                return _ORD_TYPE_LIMIT;
            else if (OrdType == OrdType.Market)
                return _ORD_TYPE_MARKET;
            //else if (OrdType == OrdType.MarketIfTouched)
            //    return _ORD_MARKET_IF_TOUCHED;
            //else if (OrdType == OrdType.LimitIfTouched)
            //    return _ORD_LIMIT_IF_TOUCHED;
            //else if (OrdType == OrdType.MarketWithLeftOverAsLimit)
            //    return _ORD_MARKET_WITH_LEFTOVER_AS_LIMIT;
            else if (OrdType == OrdType.StopLimit)
                return _ORD_TYPE_STOP_LIMIT;
            else if (OrdType == OrdType.Stop)
                return _ORD_TYPE_STOP;
            else
                throw new Exception(string.Format("Ord Type {0} not implemented", OrdType));
        }

        public string GetTIF()
        {
            return _TIF_GTC;
        }

        public void AssignExecInst(string pExecpInst)
        {

            if (ExecInst == null)
                ExecInst = "";


            if (ExecInst.Length == 0)
                ExecInst = pExecpInst;
            else
                ExecInst += "," + pExecpInst;


        }


        #region Validations

        public void ValidateOrder()
        {
            if (OrdType == OrdType.Limit && !Price.HasValue)
                throw new Exception("You must specify an price for a limit order");

            //if (OrdType == OrdType.MarketIfTouched && Price.HasValue)
            //    throw new Exception("Market if touched orders must not have a price");

            //if (OrdType == OrdType.MarketWithLeftOverAsLimit && !Price.HasValue)
            //    throw new Exception("Market if touched orders must have a price");

            if (OrdType == OrdType.Limit && !Price.HasValue)
                throw new Exception("Limit if touched orders must have a price");

            if (OrdType == OrdType.Stop && Price.HasValue)
                throw new Exception("Stop order will be a market order so it must not have a price. Use Stop Limit instead");

            if (OrdType == OrdType.StopLimit && !Price.HasValue)
                throw new Exception("Stop limit order must have a price");
        }

        #endregion

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;

namespace zHFT.OrderRouters.Bloomberg.Common.DTOs
{
    public class OrderDTO
    {
        #region Public Attributes

        public string ClOrderId { get; set; }

        public int OrderId { get; set; }

        public string Ticker { get; set; }

        public int OrderQty { get; set; }

        public double Price { get; set; }

        public string Exchange { get; set; }

        public string Broker { get; set; }

        public OrdType OrdType { get; set; }

        public Side Side { get; set; }

        public TimeInForce TimeInForce { get; set; }

        public SecurityType SecurityType { get; set; }

        #endregion

        #region Public Methods

        public string GetFullBloombergSymbol()
        {
            string secType = "";

            if (SecurityType == SecurityType.CS)
                secType = "Equity";
            else
                throw new Exception(string.Format("Security Type not implemented for Bloomberg: {0}", SecurityType.ToString()));


            return string.Format("{0} {1} {2}", Ticker, Exchange, secType);
        
        }

        public string GetSide()
        {
            if (Side == Side.Buy)
                return "BUY";
            else if (Side == Side.Sell)
                return "SELL";
            else
                throw new Exception(string.Format("Side not recognized on Bloomberg: {1}", Side));
        }

        public string GetOrdType()
        {
            if (OrdType == OrdType.Limit)
                return "LMT";
            else if (OrdType == OrdType.Market)
                return "MKT";
            else
                throw new Exception(string.Format("OrdType not recognized on Bloomberg: {1}", OrdType));
        }

        public string GetTimeInForce()
        {
            if (TimeInForce == TimeInForce.Day)
                return "DAY";
            else if (TimeInForce == TimeInForce.GoodTillCancel)
                return "GTC";
            else
                throw new Exception(string.Format("TimeInForce not recognized on Bloomberg: {1}", TimeInForce));
  
        
        }

        #endregion
    }
}

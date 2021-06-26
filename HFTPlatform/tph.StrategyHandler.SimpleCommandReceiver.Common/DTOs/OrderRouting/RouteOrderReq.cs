using System;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class RouteOrderReq:WebSocketMessage
    {
        #region Protected Consts

        protected static string _BUY = "BUY";

        protected static string _SELL = "SELL";
        
        #endregion
        
        #region Public Attributes
        
        public string ReqId { get; set; }
        
        public string UUID { get; set; }

        public string ClOrdId { get; set; }

        public string Symbol { get; set; }

        public string Side { get; set; }

        public double Qty { get; set; }

        public string  Account  { get; set; }

        public double? LimitPrice { get; set; }

        public string Currency { get; set; }

        #endregion
        
        #region Public Methods

        public Side GetSide()
        {
            if (Side == _BUY)
                return zHFT.Main.Common.Enums.Side.Buy;
            else if (Side == _SELL)
                return zHFT.Main.Common.Enums.Side.Sell;
            else
                throw new Exception(string.Format("Not recognize Side {0} @RouteOrderReq", Side));
        }

        #endregion
    }
}
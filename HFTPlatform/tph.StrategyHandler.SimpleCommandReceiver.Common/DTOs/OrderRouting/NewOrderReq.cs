using System;
using zHFT.Main.Common.Enums;

namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting
{
    public class NewOrderReq:WebSocketMessage
    {

        #region Constructors



        public NewOrderReq()
        {
            Msg = "NewOrderReq";
        }

        #endregion
        
        #region Protected Consts

        public static string _BUY = "BUY";

        public static string _SELL = "SELL";
        
        public static string _ORD_TYPE_LIMIT = "LIMIT";

        public static string _ORD_TYPE_MKT = "MKT";

        public static string _TIF_GTC = "GTC";

        public static string _TIF_DAY = "DAY";

        public static string _TIF_UNK = "UNK";
        
        #endregion
        
        #region Public Attributes
        
        public string ReqId { get; set; }
        
        public string UUID { get; set; }

        public string ClOrdId { get; set; }

        public string Symbol { get; set; }

        public string Side { get; set; }

        public double Qty { get; set; }

        public string  Account  { get; set; }
        
        public string Type { get; set; }

        public double? Price { get; set; }

        public string Currency { get; set; }

        public string Exchange { get; set; }

        public string TimeInForce { get; set; }
        
        public DateTime CreationTime { get; set; }

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

        public OrdType GetOrdType()
        {

            if (Type == _ORD_TYPE_LIMIT)
                return OrdType.Limit;
            else if (Type == _ORD_TYPE_MKT)
                return OrdType.Market;
            else
            {
                throw new Exception($"Unknown order type {Type}");
            }

        }
        
        public TimeInForce GetTimeInforce()
        {

            if (Type == _TIF_DAY)
                return zHFT.Main.Common.Enums.TimeInForce.Day;
            else if (Type == _TIF_GTC)
                return  zHFT.Main.Common.Enums.TimeInForce.GoodTillCancel;
            else
            {
                return zHFT.Main.Common.Enums.TimeInForce.Day;
            }

        }

        public NewOrderReq Clone()
        {
            NewOrderReq cloned = new NewOrderReq();

            cloned.ClOrdId = ClOrdId;
            cloned.Account = Account;
            cloned.Currency = Currency;
            cloned.Price = Price;
            cloned.Type = Type;
            cloned.Side = Side;
            cloned.Symbol = Symbol;
            cloned.Qty = Qty;
            cloned.UUID = UUID;
            cloned.ReqId = ReqId;

            return cloned;

        }

        #endregion
    }
}
namespace tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs
{
    public class WebSocketSubscribeMessage:WebSocketMessage
    {
        #region Public Static Consts

        public static string _SUSBSCRIPTION_TYPE_SUBSCRIBE = "S";

        public static string _SUSBSCRIPTION_TYPE_UNSUBSCRIBE = "U";
        
        public static string _ORDER_BOOK_SERVICE = "OB";
        
        public static string _MARKET_DATA_SERVICE = "MD";
        
        public static string _CANDLEBAR_SERVICE = "CB";

        #endregion

        #region Public Attributes

        public string SubscriptionType { get; set; }

        //public string UserId { get; set; }

        //public string JsonWebToken { get; set; }

        public string UUID { get; set; }

        public string Service { get; set; }

        public string ServiceKey { get; set; }

        #endregion
    }
}
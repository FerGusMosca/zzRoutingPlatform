using zHFT.Main.Common.Enums;

namespace tph.InstructionBasedMarketClient.IB.Common.DTO
{
    public class HistoricalPricesRequestDTO
    {

        #region Protected Static Consts

        public static string _QUERY_TIME_DATEFORMAT = "yyyyMMdd-HH:mm:ss";

        public static string _TRADES = "TRADES";

        public static string _INT_1_MIN = "1 min";
        
        public static string _INT_5_MIN = "5 min";
        
        public static string _INT_1_HOUR = "1 hour";
        
        public static string _INT_5_HOUR = "5 hour";
        
        public static string _INT_DAY = "1 day";

        #endregion
        
        #region Public Attributes
        
        public  int ReqId { get; set; }
        
        public  string Symbol { get; set; }
        
        public  string Currency { get; set; }
        
        public  SecurityType SecurityType { get; set; }
        

        public string Exchange { get; set; }
        public  string QueryTime { get; set; }// TO in yyyyMMdd-HH:mm:ss format
        
        // The amount of time (or Valid Duration String units) to go back from the request's given end date and time.
        // 10 D, 1 M
        public string DurationString { get; set; }
        
        public string BarSize { get; set; }
        
        public string WhatToShow { get; set; }
        
        #endregion
    }
}
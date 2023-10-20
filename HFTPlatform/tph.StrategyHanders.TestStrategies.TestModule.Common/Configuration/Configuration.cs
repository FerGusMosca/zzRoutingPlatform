using System.Collections.Generic;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.Interfaces;

namespace tph.StrategyHanders.TestStrategies.TestModule.Common.Configuration
{
    public class Configuration:BaseConfiguration
    {

        #region Public Static Consts

        public static string _ACTION_ROUTE_MARKET = "ROUTE_MARKET";
        
        public static string _ACTION_ROUTE_CASH = "ROUTE_CASH";

        public static string _ACTION_MARKET_DATA_REQUEST = "MARKET_DATA_REQUEST";
        
        public static string _ACTION_HISTORICAL_RICES_REQUEST = "HISTORICAL_PRICES_REQUEST";
        
        public static string _ACTION_CANCEL_LAST_POSITION = "CANCEL_LAST_POSITION";

        public static string _SIDE_BUY = "BUY";

        public static string _SIDE_SELL = "SELL";
        

        #endregion

        #region Public Attributes

        public string Name { get; set; }
        
        public string Action { get; set; }
        
        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        public string Symbol { get; set; }

        public string Currency { get; set; }

        public string Side { get; set; }

        public double SellQty { get; set; }

        public double BuyQty { get; set; }
        
        #endregion

        public override bool CheckDefaults(List<string> result)
        {

            if (string.IsNullOrEmpty(Action))
                return false;
            
            
            return true;
        }
    }
}
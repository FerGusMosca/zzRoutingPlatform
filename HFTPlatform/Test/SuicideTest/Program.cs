

using Bittrex;
using Newtonsoft.Json.Linq;

namespace SuicideTest
{
    internal class Program
    {
        private static void DoTestConn()
        {
            Exchange exch = new Exchange();
            
            ExchangeContext ctx= new ExchangeContext()
            {
                ApiKey ="",
                QuoteCurrency = "USDT",
                Secret = "",
                Simulate = false
            };
            
            
           
            //ctx.QuoteCurrency = "ETH";
            exch.Initialise(ctx);
            
            JObject jMarketData = exch.GetTicker("ETH");
//            
        }

        
        public static void Main(string[] args)
        {
            DoTestConn();
        }
    }
}
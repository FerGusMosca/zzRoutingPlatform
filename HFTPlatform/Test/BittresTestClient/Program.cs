using Bittrex;
using Newtonsoft.Json.Linq;

namespace BittresTestClient
{
    internal class Program
    {
        private static void DoTestConn()
        {
            Exchange exch = new Exchange();
            
            ExchangeContext ctx= new ExchangeContext()
            {
                ApiKey ="ad6ec72057b24afd9a5f292da4d8b496",
                QuoteCurrency = "USDT",
                Secret = "38ebce53581d44e38023861fbdf3a910",
                Simulate = false
            };
            
            
           
            //ctx.QuoteCurrency = "ETH";
            exch.Initialise(ctx);
            
            JObject jMarketData = exch.GetTicker("ETH");
            
            //  Program.cs(25, 35): [CS0656] Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.Binder.Convert'
            
        }

        
        
        public static void Main(string[] args)
        {
            DoTestConn();
        }
    }
}
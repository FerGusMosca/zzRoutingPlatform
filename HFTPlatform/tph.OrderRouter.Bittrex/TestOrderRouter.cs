//using System;
//using Bittrex.Net.Clients;
//using CryptoExchange.Net.Authentication;
//using Microsoft.Extensions.Logging;

//namespace tph.OrderRouter.Bittrex
//{
//    public class TestOrderRouter
//    {
//        public void TestRouting()
//        {
     
//            var bittrexRestClient = new BittrexRestClient(options =>
//            {
//                options.ApiCredentials = new ApiCredentials("ad6ec72057b24afd9a5f292da4d8b496", "38ebce53581d44e38023861fbdf3a910");
//                options.RequestTimeout = TimeSpan.FromSeconds(60);
//            });

//            //Subscribe order book
//            var orderBookData = bittrexRestClient.SpotApi.ExchangeData.GetOrderBookAsync("BTC-USDT").Result;


//            //Send Order

//            //Update Order

//            //Refresh execution report
//        }

//    }
//}
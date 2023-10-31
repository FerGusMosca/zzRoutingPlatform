
using System;
using tph.OrderRouter.Bittrex;
using zHFT.Main.Common.Interfaces;

namespace CryptoAlgoTest
{
    internal class Program
    {
        private static void DoTestConn()
        {
            //TestOrderRouter router = new TestOrderRouter();

            //router.TestRouting();

            string assembly = "tph.OrderRouter.Bittrex.OrderRouter, tph.OrderRouter.Bittrex";

            var orderProxyType = Type.GetType(assembly);
            if (orderProxyType != null)
            {
                var OrderProxy = (ICommunicationModule)Activator.CreateInstance(orderProxyType);
                OrderProxy.Initialize(null, null, null);
            }
            else
                throw new Exception("assembly not found: " + assembly);
            //            
        }

        
        public static void Main(string[] args)
        {
            DoTestConn();
        }
    }
}
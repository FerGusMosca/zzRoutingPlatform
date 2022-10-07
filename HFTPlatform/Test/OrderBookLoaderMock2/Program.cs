using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using OrderBookLoaderMock.Common.DTO;
using OrderBookLoaderMock.Common.DTO.Orders;
using OrderBookLoaderMock.Common.Interfaces;
using OrderBookLoaderMock.LogicLayer;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.MarketData;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs.OrderRouting;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace OrderBookLoaderMock2
{
    internal class Program:IMarketDataPublication,IOnExecutionReport,ILogger
    {
        #region Protected Attributes
        protected Logger Logger { get; set; }
        
        protected MarketClientLogic MarketClientLogic { get; set; }
        
        #endregion

        #region Public  Methods
        public void DoLog(string msg, Constants.MessageType type)
        {
            Logger.DoLog(msg,type);
        }

        public void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
        }

        #endregion

        public  void ProcessWarning(string warning)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(warning);
            Console.WriteLine("");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public  void ProcessEvent(WebSocketMessage msg)
        {
            if (msg.Msg == "OrderBookMsg")
            {
                OrderBookMsg ob = (OrderBookMsg) msg;

                if (ob.Bids.Length != 5)
                    ProcessWarning(string.Format("WARNING-Received bids with more/less than 5 orders: {0}",ob.Bids.Length));
                
                if (ob.Asks.Length != 5)
                    ProcessWarning(string.Format("WARNING-Received asks with more/less than 5 orders:{0}",ob.Asks.Length));
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine(string.Format("=============Order Book for Symbol {0} ===============",ob.Security.Symbol));
                Console.WriteLine(string.Format(ob.GetStrEntry(0)));
                Console.WriteLine(string.Format(ob.GetStrEntry(1)));
                Console.WriteLine(string.Format(ob.GetStrEntry(2)));
                Console.WriteLine(string.Format(ob.GetStrEntry(3)));
                Console.WriteLine(string.Format(ob.GetStrEntry(4)));
                Console.WriteLine("");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;

            }
            else if (msg.Msg == "CandlebarMsg")
            {
                CandlebarMsg cb = (CandlebarMsg) msg;
                Console.WriteLine(string.Format("=============New Candle for Symbol {0} ===============",cb.Symbol));
                Console.WriteLine(string.Format(
                    " Key={6} DateTime={7} Open={0} High={1} Low={2} Close={3} Trade={4} Volume={5}",
                    cb.Open.HasValue ? cb.Open.Value.ToString("##.##") : "-",
                    cb.High.HasValue ? cb.High.Value.ToString("##.##") : "-",
                    cb.Low.HasValue ? cb.Low.Value.ToString("##.##") : "-",
                    cb.Close.HasValue ? cb.Close.Value.ToString("##.##") : "-",
                    cb.Trade.HasValue ? cb.Trade.Value.ToString("##.##") : "-",
                    cb.Volume, cb.Key, cb.Date.ToString("yyyy-MM-dd hh:mm:ss"))
                );



            }
        }

        public   void OnMarketData(MarketDataMsg msg)
        {
            Console.WriteLine(string.Format("Market Data: {0}",msg.ToString()));
        }

        public void OnExecutionReport(ExecutionReportMsg msg)
        {
            Console.WriteLine(string.Format("Execution Report Received!: {0}",msg.ToString()));
        }

        static void Main(string[] args)
        {
            Program logger = new Program() {Logger = new Logger(ConfigurationManager.AppSettings["DebugLevel"])};
            try
            {
                string mockWS=ConfigurationManager.AppSettings["MockWS"];
                
                logger.DoLog("Initializing Order Book Loader Mock App", Constants.MessageType.Information);

                
                logger.MarketClientLogic= new MarketClientLogic(mockWS,logger,logger,logger);
                
                //#1 - Test Order Book Subscription
                //logger.MarketClientLogic.SubscribeMarketData("EUR$GBP.IDEALPRO.CASH");
                //logger.MarketClientLogic.SubscribeMarketData("AAPL");
                logger.MarketClientLogic.SubscribeCandlebars("AAPL");
                
                //#2- Send Orders
                //logger.MarketClientLogic.SendLimitOrder("AAPL", NewOrderReq._BUY, 1, 100);

                logger.DoLog(string.Format("Existing Order Book Loader Mock processed"), Constants.MessageType.Information);

            }
            catch (Exception e)
            {
                logger.DoLog(string.Format("Critical error initializing Order Book loader mock:{0}",e.Message),Constants.MessageType.Error);
            }
            
            Thread.Sleep(Timeout.Infinite);
            
        }
    }
}
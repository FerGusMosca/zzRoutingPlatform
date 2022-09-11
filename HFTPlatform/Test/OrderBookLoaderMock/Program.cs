using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
g;
using OrderBookLoaderMock.Common.DTO;
using OrderBookLoaderMock.Common.DTO.Orders;
using OrderBookLoaderMock.Common.Interfaces;
using tph.StrategyHandler.SimpleCommandReceiver.Common.DTOs;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace OrderBookLoaderMock
{
    class Program:ILogger,IMarketDataPublication
    {
        #region Protected Attributes
        protected Logger Logger { get; set; }
        
        #endregion

        #region Protected Static Attributes
        
        protected static HistoricalPricesUpdateDTO[] SecMappings { get; set; }
        
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

        public void ProcessWarning(string warning)
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
                

                HistoricalPricesUpdateDTO sec = SecMappings.Where(x => x.RealSymbol == ob.Security.Symbol).FirstOrDefault();
                
                //TODO map, ATS security
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine(string.Format("=============Order Book for Symbol {0} ===============",sec.ATSSymbol));
                Console.WriteLine(string.Format(ob.GetStrEntry(0)));
                Console.WriteLine(string.Format(ob.GetStrEntry(1)));
                Console.WriteLine(string.Format(ob.GetStrEntry(2)));
                Console.WriteLine(string.Format(ob.GetStrEntry(3)));
                Console.WriteLine(string.Format(ob.GetStrEntry(4)));
                Console.WriteLine("");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;

            }
        }

        public void OnMarketData(MarketDataMsg msg)
        {
            Console.WriteLine(string.Format("Market Data: {0}",msg.ToString()));
        }


        public void OnExecutionReport(ExecutionReportMsg msg)
        {
            Console.WriteLine(string.Format("Execution Report: {0}",msg.ToString()));
        }

        static void Main(string[] args)
        {
            Program logger = new Program() {Logger = new Logger(ConfigurationManager.AppSettings["DebugLevel"])};
            try
            {
                string appName = ConfigurationManager.AppSettings["ApplicationName"];
                string ordersCS = ConfigurationManager.AppSettings["OrdersDBConnectionString"];
                string tradingCS = ConfigurationManager.AppSettings["TradingDBConnectionString"];
                
                string mockWS=ConfigurationManager.AppSettings["MockWS"];
                
                logger.DoLog("Initializing Order Book Loader Mock App", Constants.MessageType.Information);

                string secMappingJSON = File.ReadAllText(ConfigurationManager.AppSettings["InputSecurityFile"]);
                string shareholderMappingJSON = File.ReadAllText(ConfigurationManager.AppSettings["InputShareholderFile"]);
                
                SecMappings = JsonConvert.DeserializeObject<HistoricalPricesUpdateDTO[]>(secMappingJSON);
                ShareholderDTO shareholderMapping = JsonConvert.DeserializeObject<ShareholderDTO>(shareholderMappingJSON);

                MarketDataLogic mdLogic = new MarketDataLogic(appName, tradingCS, ordersCS, mockWS,
                                                               SecMappings,shareholderMapping ,logger,logger);
                
                //mdLogic.SubscribeMarketData("BTC");//use account 9732
                //mdLogic.RouteOrder("GGAL.BUE.CS", "BUY", 1, 130, "9732");

                foreach (HistoricalPricesUpdateDTO secMap in SecMappings)
                {
                    mdLogic.SubscribeOrderBook(secMap.RealSymbol);
                }
                
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
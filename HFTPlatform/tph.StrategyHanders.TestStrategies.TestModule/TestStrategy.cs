using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tph.StrategyHanders.TestStrategies.TestModule.Common.Configuration;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Common.Common.Wrappers;
using zHFT.MarketClient.Common.Wrappers;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;
using static zHFT.Main.Common.Util.Constants;

namespace tph.StrategyHanders.TestStrategies.TestModule
{
  

    public class TestStrategy : BaseCommunicationModule,ILogger
    {

        #region Protected Attr  

        protected Configuration Config { get; set; }

        protected ICommunicationModule OrderRouter { get; set; }

        protected int NextPosId { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        protected bool Routing { get; set; }

        protected bool RecvMarketData { get; set; }
        
        protected Position LastRoutedPosition { get; set; }

        #endregion

        #region Private Methods

        private Security GetSecurity()
        {

            return new Security() { Symbol = Config.Symbol ,Currency=Config.Currency};
        
        }

        private void RequestMarketData()
        {

            Security sec = GetSecurity();


            MarketDataRequestWrapper wrapper = new MarketDataRequestWrapper( sec, SubscriptionRequestType.SnapshotAndUpdates, sec.Currency);
            MarketDataRequestCounter++;
            OnMessageRcv(wrapper);
        }
        
        private void RequestHistoricalPrices()
        {

            TimeSpan elapsed = DateTime.Now - new DateTime(1970, 1, 1);

            DateTime to = DateTime.Now;
            DateTime from = to.AddDays(-1);
            string symbol = Config.Symbol;
            CandleInterval interval = CandleInterval.Minute_1;

            HistoricalPricesRequestWrapper wrapper = new HistoricalPricesRequestWrapper(
                Convert.ToInt32(elapsed.TotalSeconds),
                symbol, from, to, interval);
            
            OnMessageRcv(wrapper);


        }

        private void ProcessExecutionReport(Wrapper wrapper)
        {
            ExecutionReportConverter converter = new ExecutionReportConverter();

            ExecutionReport execRep= converter.GetExecutionReport(wrapper, Config);

            if (!execRep.IsActiveOrder())
            {
                Routing = false;
                DoLog($"Finished routing with {execRep.OrdStatus} execution report", MessageType.Information);
            }


            DoLog($"{Config.Name}--> Recv Exec. Report:{execRep.ToString()}", MessageType.Information);

        }

        private void RouteNewBuyTestOrder()
        {

            Position pos = new Position()
            {
                Security = GetSecurity(),
                Side = Side.Buy,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = Config.BuyQty,
                QuantityType = QuantityType.CURRENCY,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = null,
            };

            //pos.Security.MarketData = new MarketData() { Currency = pos.Security.Currency };
            
            pos.LoadPosId(NextPosId);
            NextPosId++;
            Routing = true;
            PositionWrapper posWrapper = new PositionWrapper(pos, Config);

            CMState state = OrderRouter.ProcessMessage(posWrapper);

            //Thread.Sleep(12000);//2 seconds

            //CancelPositionWrapper cxlWrapper = new CancelPositionWrapper(pos, Config);
            //CMState state2 = OrderRouter.ProcessMessage(cxlWrapper);

        }
        
        private void RouteOrder(Side side, bool cashQty)
        {
            Position pos = new Position()
            {
                Security = GetSecurity(),
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty =side==Side.Buy? Config.BuyQty:Config.SellQty,//Cash if there is no price
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = "TestStrategyAcc",
            };
            
         

            if (side == Side.Buy)
            {
                pos.QuantityType =cashQty ? QuantityType.CURRENCY : QuantityType.SHARES;

                if (cashQty)
                    pos.CashQty = Config.BuyQty;

            }
            else if (side==Side.Sell)
            {
                pos.QuantityType =  QuantityType.SHARES;
            }
            else
            {
                throw new Exception($"Invalid side {side}");
            }


            pos.LoadPosId(NextPosId);
            NextPosId++;
            Routing = true;
            LastRoutedPosition = pos;
            PositionWrapper posWrapper = new PositionWrapper(pos, Config);
            CMState state = OrderRouter.ProcessMessage(posWrapper);
        }


        private void CancelLastPosition()
        {
            if (LastRoutedPosition != null)
            {

                CancelPositionWrapper posToCxlWrapper=new CancelPositionWrapper(LastRoutedPosition, Config);
                DoLog($"Cancel Position {LastRoutedPosition.PosId} on market",MessageType.Information);
                CMState state = OrderRouter.ProcessMessage(posToCxlWrapper);
            }
            else
            {
                DoLog($"Could not cancel a position because no position has ben routed!", MessageType.Error);
            }


        }

        //OptionChainRequest
        private void OptionChainRequest()
        {
            SecurityListRequestWrapper slrWrapper = new SecurityListRequestWrapper(
                                                                                    SecurityListRequestType.OptionChain,
                                                                                    Config.Symbol,
                                                                                    SecurityType.CS,
                                                                                    null,
                                                                                    "USD"
                                                                                    );

            DoLog($"{Config.Name}-->Requesting option change for symbol {Config.Symbol} ", MessageType.Information);
            CMState state = OnMessageRcv(slrWrapper);
        }

        private void RouteNewSellTestOrder()
        {

            Position pos = new Position()
            {
                Security = GetSecurity(),
                Side = Side.Sell,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                Qty = Config.SellQty,
                QuantityType = QuantityType.SHARES,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = null,
            };

            //pos.Security.MarketData = new MarketData() { Currency = pos.Security.Currency };
            pos.LoadPosId(NextPosId);
            NextPosId++;
            Routing = true;
            PositionWrapper posWrapper = new PositionWrapper(pos, Config);

            CMState state = OrderRouter.ProcessMessage(posWrapper);

            //Thread.Sleep(12000);//2 seconds

            //CancelPositionWrapper cxlWrapper = new CancelPositionWrapper(pos, Config);
            //CMState state2 = OrderRouter.ProcessMessage(cxlWrapper);

        }

        private void InitializeModules(OnLogMessage pOnLogMsg)
        {
            DoLog("Initializing Order Router " + Config.OrderRouter, Constants.MessageType.Information);
            if (!string.IsNullOrEmpty(Config.OrderRouter))
            {
                var typeOrderRouter = Type.GetType(Config.OrderRouter);
                if (typeOrderRouter != null)
                {
                    OrderRouter = (ICommunicationModule)Activator.CreateInstance(typeOrderRouter);
                    OrderRouter.Initialize(ProcessOutgoing, pOnLogMsg, Config.OrderRouterConfigFile);
                }
                else
                    throw new Exception("assembly not found: " + Config.OrderRouter);
            }
            else
                DoLog("Order Router not found. It will not be initialized", Constants.MessageType.Error);

        }

        protected CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                {
                    ProcessExecutionReport(wrapper);

                    return CMState.BuildSuccess();
                }


                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Config.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + Config.Name + ":" + ex.Message, MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        private async void WaitForMarketDataAndRoute()
        {
            try
            {
//                while (!RecvMarketData)
//                {
//                    Thread.Sleep(100);
//                }

                if (Config.Side == "BUY")
                    RouteNewBuyTestOrder();
                else
                    RouteNewSellTestOrder();
            }
            catch (Exception ex) {

                DoLog($"CRITICAL ERROR ROUTING TO MARKET :{ex.Message}", MessageType.Error);
            
            }
        }

        protected void ProcessMarketData(Wrapper wrapper)
        {
            MarketDataWrapper mdWrapper = (MarketDataWrapper) wrapper;

            DoLog($"{mdWrapper.ToString()}", MessageType.Information);

        }


        protected void ProcessHistoricalPrices(Wrapper wrapper)
        {

            HistoricalPricesWrapper histWrapper = (HistoricalPricesWrapper) wrapper;

           Security sec = (Security) histWrapper.GetField(HistoricalPricesFields.Security);
           List<Wrapper> prices = (List<Wrapper>) histWrapper.GetField(HistoricalPricesFields.Candles);


           DoLog($"Received prices for security {sec.Symbol}", MessageType.Information);

           foreach (Wrapper wr in prices)
           {
               MarketDataWrapper mdWrapper = (MarketDataWrapper) wr;

               DoLog($"{mdWrapper.ToString()}", MessageType.Information);
           }
        }

        protected void OnRouteCash()
        {
            RequestMarketData();
            
            while (!RecvMarketData)
            {
                Thread.Sleep(100);
            }
            
            if (Config.Side == Configuration._SIDE_BUY)
                RouteOrder(Side.Buy,true);
            else if(Config.Side==Configuration._SIDE_SELL)
                RouteOrder(Side.Sell,true);
            else
                DoLog($"@{Config.Name}--> Side not implemented:{Config.Side}",MessageType.Error);
        }

        protected void EvalActions()
        {
            Thread.Sleep(3000);//WAIT for the websocket to connect 
            if (Config.Action == Configuration._ACTION_ROUTE_MARKET)
            {
                
                if (Config.Side == Configuration._SIDE_BUY)
                    RouteOrder(Side.Buy,false);
                else if(Config.Side==Configuration._SIDE_SELL)
                    RouteOrder(Side.Sell,false);
                else
                    DoLog($"@{Config.Name}--> Side not implemented:{Config.Side}",MessageType.Error);
            }
            else  if (Config.Action == Configuration._ACTION_ROUTE_CASH)
            {
                OnRouteCash();
            }
            else if (Config.Action == Configuration._ACTION_MARKET_DATA_REQUEST)
            {

                RequestMarketData();
            }
            else if (Config.Action == Configuration._ACTION_HISTORICAL_RICES_REQUEST)
            {

                RequestHistoricalPrices();
            }
            else if (Config.Action == Configuration._ACTION_CANCEL_LAST_POSITION)
            {
                OnRouteCash();
                Thread.Sleep(10000);
                CancelLastPosition();
            }
            else if (Config.Action == Configuration._ACTION_OPTION_CHAIN_REQUEST)
            {
                
                OptionChainRequest();
            }
            else
            {
                
                DoLog($"@{Config.Name}-->Action not implemented !:{Config.Action}",MessageType.Error);
            }
        }

        #endregion

        #region Overriden Methods
        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            //this.ModuleConfigFile = configFile;
            this.OnMessageRcv += pOnMessageRcv;
            this.OnLogMsg += pOnLogMsg;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                NextPosId =1;
                Routing = false;
                RecvMarketData= false;
                MarketDataRequestCounter = 1;
                InitializeModules(pOnLogMsg);
                //RequestMarketData();
                
                //RequestHistoricalPrices();

                //WaitForMarketDataAndRoute();

                EvalActions();

                return true;

            }
            else
            {
                return false;
            }
        }

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    RecvMarketData = true;
                    if (Routing)
                    {
                        DoLog("Processing Market Data:" + wrapper.ToString(), MessageType.Information);
                        OrderRouter.ProcessMessage(wrapper);
                    }

                    ProcessMarketData(wrapper);

                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.HISTORICAL_PRICES)
                {
                    ProcessHistoricalPrices(wrapper);

                    return CMState.BuildSuccess();
                }

               
                else
                    return CMState.BuildFail(new Exception(string.Format("Could not process action {0} for strategy {1}", wrapper.GetAction().ToString(), Config.Name)));
            }
            catch (Exception ex)
            {
                DoLog("Error processing market data @" + Config.Name + ":" + ex.Message,MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            Config = ConfigLoader.GetConfiguration<Configuration>(this, configFile, listaCamposSinValor);
        }

        void ILogger.DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            DoLoadConfig(configFile, listaCamposSinValor);
        }

        void ILogger.DoLog(string msg, Constants.MessageType type)
        {
            
        }

        #endregion
    }
}

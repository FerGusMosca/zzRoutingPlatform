using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;
using static zHFT.Main.Common.Util.Constants;

namespace tph.StrategyHanders.TestStrategies.TestModule
{
    public class Configuration:IConfiguration
    { 

        public string Name { get; set; }
        public string OrderRouter { get; set; }

        public string OrderRouterConfigFile { get; set; }

        public string Symbol { get; set; }

        public string Currency { get; set; }

        public string Side { get; set; }

        public double SellQty { get; set; }

        public double BuyQty { get; set; }

        public bool CheckDefaults(List<string> result)
        {
            return true;
        }
    }

    public class TestStrategy : BaseCommunicationModule,ILogger
    {

        #region Protected Attr  

        protected Configuration Config { get; set; }

        protected ICommunicationModule OrderRouter { get; set; }

        protected int NextPosId { get; set; }

        protected int MarketDataRequestCounter { get; set; }

        protected bool Routing { get; set; }

        protected bool RecvMarketData { get; set; }

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

        private void ProcessExecutionReport(Wrapper wrapper)
        {
            ExecutionReportConverter converter = new ExecutionReportConverter();

            ExecutionReport execRep= converter.GetExecutionReport(wrapper, Config);

            if (!execRep.IsActiveOrder())
            {
                Routing = false;
                DoLog($"Finished routing with {execRep.OrdStatus} execution report", MessageType.Information);
            }

        
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
                while (!RecvMarketData)
                {
                    Thread.Sleep(100);
                }

                if (Config.Side == "BUY")
                    RouteNewBuyTestOrder();
                else
                    RouteNewSellTestOrder();
            }
            catch (Exception ex) {

                DoLog($"CRITICAL ERROR ROUTING TO MARKET :{ex.Message}", MessageType.Error);
            
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
                RequestMarketData();

                WaitForMarketDataAndRoute();

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

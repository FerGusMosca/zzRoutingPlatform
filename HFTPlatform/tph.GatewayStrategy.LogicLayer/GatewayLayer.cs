using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Interfaces;
using zHFT.StrategyHandler.Common.Wrappers;
using zHFT.StrategyHandler.LogicLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using static zHFT.Main.Common.Util.Constants;
using zHFT.Main.Common.Wrappers;
using zHFT.StrategyHandlers.Common.Converters;
using System.Threading;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using tph.GatewayStrategy.Common.Configuration;
using zHFT.OrderRouters.Common.Converters;
using zHFT.Main.BusinessEntities.Orders;
using System.Security.AccessControl;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.StrategyHandler.BusinessEntities;


namespace tph.GatewayStrategy.LogicLayer
{
    public class GatewayLayer : DayTradingStrategyBase
    {
        #region Protected Attributes

        protected GatewayConfiguration Config { get;set; }

        protected Dictionary<string, MarketData> MarketDataDict { get; set; }

        protected Dictionary<string, Position> RoutedPosDict { get; set; }

        #endregion


        #region Potected Methods


        private Security GetSecurity(string symbol, string currency, string exchange, SecurityType? secType)
        {
            return new Security()
            {
                Symbol = symbol,
                Currency = currency,
                Exchange = exchange,
                SecType = secType.HasValue ? secType.Value : SecurityType.CS,

            };
        }

        protected virtual Position LoadNewRegularPos(Security sec, Side side, QuantityType qtyType, double? qty, double? cashQty, string accountId)
        {

            Position pos = new Position()
            {

                Security = sec,
                Exchange = sec.Exchange,
                Side = side,
                PriceType = PriceType.FixedAmount,
                NewPosition = true,
                CashQty = qtyType == QuantityType.CURRENCY ? (double?)cashQty : null,
                Qty = qtyType == QuantityType.SHARES ? (double?)qty : null,
                QuantityType = qtyType,
                PosStatus = zHFT.Main.Common.Enums.PositionStatus.PendingNew,
                AccountId = accountId,


            };

            pos.LoadPosGuid(PositionIdTranslator.GetNextGuidPosId());

            return pos;
        }


        private void DoRequestMarketData(Order newOrder)
        {
            Security sec = GetSecurity(newOrder.Security.Symbol, newOrder.Currency,  newOrder.Exchange, newOrder.Security.SecType);
            MarketDataRequestWrapper mdrWrapper = new MarketDataRequestWrapper(sec, SubscriptionRequestType.SnapshotAndUpdates, sec.Currency);
            IncomingModule.ProcessMessage(mdrWrapper);
        }

        private void DoOpenTradingRegularPos(Position pos)
        {
            PositionWrapper posWrapper = new PositionWrapper(pos, Config);
            OrderRouter.ProcessMessage(posWrapper);
        }

        private void LoadNewPos(Order newOrder)
        {
            Security sec = GetSecurity(newOrder.Security.Symbol, newOrder.Currency, newOrder.Exchange, newOrder.Security.SecType);
            Position pos = LoadNewRegularPos(sec, newOrder.Side, newOrder.QuantityType, newOrder.OrderQty,newOrder.CashOrderQty,newOrder.Account);

            DoLog($"ROUTING Pos for Symbol {pos.Security.Symbol} Side={newOrder.Side} CashQty={newOrder.CashOrderQty} ", Constants.MessageType.PriorityInformation);
            lock (RoutedPosDict)
            {

                RoutedPosDict.Add(pos.PosId, pos);
            }
            
            DoOpenTradingRegularPos(pos);
            DoLog($"Position sent to the exchange...", MessageType.Information);
        }

        protected CMState RoutePositionOnNewOrder(Wrapper wrapper)
        {

            try
            {
                DoLog($"Extracting order to create new position", MessageType.Debug);
                var conv = new tph.GatewayStrategy.Common.Util.OrderConverter();

                Order order = conv.ConvertNewOrder(wrapper);

                DoLog($"New order found for symbol {order.Symbol} Qty={order.OrderQty} CashQty={order.CashOrderQty} Type={order.OrdType} Price={order.Price}", MessageType.Information);
                DoLog($"Requesting market data for symbol {order.Symbol}", MessageType.Information);
                
                DoRequestMarketData(order);
                DoLog($"Building new position for symbol {order.Symbol}", MessageType.Information);
                LoadNewPos(order);
                DoLog($"Position for symbol {order.Symbol} successfully sent to the exchange", MessageType.Information);

                return CMState.BuildSuccess();


            }
            catch (Exception ex) {

                DoLog($"ERROR building new new position :{ex.Message}", MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion


        #region Overriden Methods

        public override void InitializeManagers(string connStr)
        {
            //No managers-No DB
        }

        protected override zHFT.StrategyHandler.BusinessEntities.PortfolioPosition DoOpenTradingFuturePos(zHFT.Main.BusinessEntities.Positions.Position pos, zHFT.StrategyHandler.BusinessEntities.MonitoringPosition portfPos)
        {
            throw new NotImplementedException("No Futures Trading Logic");
        }

        protected override zHFT.StrategyHandler.BusinessEntities.PortfolioPosition DoOpenTradingRegularPos(zHFT.Main.BusinessEntities.Positions.Position pos, zHFT.StrategyHandler.BusinessEntities.MonitoringPosition portfPos)
        {
            PositionWrapper posWrapper = new PositionWrapper(pos, Config);
            OrderRouter.ProcessMessage(posWrapper);
            return null;
        }

        protected override void DoPersist(zHFT.StrategyHandler.BusinessEntities.PortfolioPosition trdPos)
        {
            //No managers no DB
        }

        protected override void LoadMonitorsAndRequestMarketData()
        {

            try
            {

                DoLog($"Initializing Gateway Layer...", MessageType.PriorityInformation);
                //We load different entities
                MarketDataDict = new Dictionary<string, MarketData>();
                RoutedPosDict = new Dictionary<string, Position>();


                OrderRouter = LoadModules(Config.OrderRouter, Config.OrderRouterConfigFile, DoLog);

                IncomingModule = LoadModules(Config.IncomingModule, Config.IncomingModuleConfigFile, DoLog);


                DoLog($"Gateway Layer successfully initialized...", MessageType.PriorityInformation);
            }
            catch (Exception ex) {

                DoLog($"CRITICAL ERROR initializing Gateway Layer: {ex.Message}", MessageType.Error);
            
            }

        }

        protected override void LoadPreviousTradingPositions()
        {
            //Nothing to Load
        }

        protected override void ProcessHistoricalPrices(object pWrapper)
        {
            //Nothing to Process
        }

        protected override void ProcessMarketData(object pWrapper)
        {
            Wrapper wrapper = (Wrapper)pWrapper;
            MarketData md = zHFT.StrategyHandler.Common.Converters.MarketDataConverter.ConvertMarketData(wrapper);
            lock (MarketDataDict)
            {

                if (!MarketDataDict.ContainsKey(md.Security.Symbol))
                {
                    DoLog($"Recv first MD for symbol {md.Security.Symbol}:{md.ToString()}", MessageType.PriorityInformation);
                    MarketDataDict.Add(md.Security.Symbol, md);
                }
                else
                {
                    MarketDataDict[md.Security.Symbol] = md;
                }

            }

            OrderRouter.ProcessMessage(wrapper);
        }

        protected override void ResetEveryNMinutes(object param)
        {

        }

        protected override void DoRequestSecurityListThread(object param)
        { 
        
        
        }

        public override  CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.NEW_ORDER)
                {
                    return RoutePositionOnNewOrder(wrapper);
                }
                else
                    return base.ProcessMessage(wrapper);
            }
            catch (Exception ex)
            {
                DoLog($"ERROR @ProcessMessage of GatewayLayer {Config.Name} : {ex.Message}",MessageType.Error);
                return CMState.BuildFail(ex);
            }

        }

        #endregion


        #region Gateway Methods


        //Nothing to process on the Gateway from the incoming app which is not a message
        public CMState ProcessIncoming(Wrapper wrapper)
        {
            try
            {
                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                DoLog($"{Config.Name}-->Error processing market data: {ex.Message} ", Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }



        //From the exchange ---> We send it to the Incoming Module (outside app)
        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                    DoLog($"Incoming message from order routing w/ Action {wrapper.GetAction()}: " + wrapper.ToString(), Constants.MessageType.Information);

                return base.ProcessOutgoing(wrapper);
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }


        public override void DoLoadConfig(string configFile, List<string> noValFields)
        {

            List<string> noValueFields = new List<string>();
            Config = new GatewayConfiguration().GetConfiguration<GatewayConfiguration>(configFile, noValueFields);
        }


        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {

            OnLogMsg += pOnLogMsg;

            if (ConfigLoader.LoadConfig(this, configFile))
            {
                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);
                return true;
            }
            else
            {
                DoLog($"Error initializing config file at GatewayLayer!", MessageType.Error);
                return false;
            
            }
        }

        #endregion
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common;
using zHFT.OrderRouters.Common.Converters;
using zHFT.OrderRouters.Common.Wrappers;


namespace zHFT.OrderRouters.Router
{
    public class MarketOrderRouter : OrderRouterBase, ICommunicationModule
    {
        #region Protected Attributes

        protected ICommunicationModule OrderProxy { get; set; }

        protected PositionConverter PositionConverter { get; set; }

        protected MarketDataConverter MarketDataConverter { get; set; }

        protected ExecutionReportConverter ExecutionReportConverter { get; set; }

        protected Common.Configuration.Configuration ORConfiguration
        {
            get { return (Common.Configuration.Configuration)Config; }
            set { Config = value; }
        }

        public static object tLockCalculus { get; set; }

        public IList<Position> Positions { get; set; }

        #endregion

        #region Private Methods

        private Order BuildOrder(Position pos, Side side, int index)
        {
            Order order = new Order()
            {
                Security = pos.Security,
                ClOrdId = pos.GetNextClOrdId(index + (ORConfiguration.OrderIdStart.HasValue ? ORConfiguration.OrderIdStart.Value : 0)),
                Side = side,
                OrdType = OrdType.Market,
                TimeInForce = TimeInForce.Day,
                Currency = Currency.USD.ToString(),
                QuantityType = pos.QuantityType,
                PriceType = PriceType.FixedAmount,
                Account = pos.AccountId,
                Index = index
            };
            order.OrigClOrdId = order.ClOrdId;

            if (pos.IsMonetaryQuantity())
            {
                double qty = Math.Floor(pos.CashQty.Value / order.Price.Value);
                order.OrderQty = qty;
                pos.LeavesQty = qty;//Position Missing to fill in shares
            }
            else if (pos.IsNonMonetaryQuantity())
            {
                order.OrderQty = pos.Qty;
                pos.LeavesQty = pos.Qty;//Position Missing to fill in amount of shares
            }
            else
                throw new Exception("Could not process position quantity type: " + pos.QuantityType.ToString());

            return order;
        }

        private void RunNewPosition(Position pos)
        {
            if (pos.NewPosition)
            {
                if (pos.Side == Side.Buy)
                {
                    Order order = BuildOrder(pos, Side.Buy, 0);
                    pos.Orders.Add(order);

                    DoLog(string.Format("Creating buy order for symbol {0}.Quantity={1} Price={2}",
                                        pos.Security.Symbol,
                                        order.OrderQty.HasValue ? order.OrderQty.Value : 0,
                                        order.Price.HasValue ? order.Price.Value.ToString("##.##") : "<market>"),
                            Constants.MessageType.Information);

                    NewOrderWrapper wrapper = new NewOrderWrapper(order, Config);
                    CMState processed = OrderProxy.ProcessMessage(wrapper);
                    if (processed.Success)
                    {
                        pos.NewDomFlag = false;
                        pos.NewPosition = false;
                        pos.PositionCanceledOrRejected = false;
                        pos.PositionCleared = false;
                        pos.PosStatus = PositionStatus.PendingNew;
                    }
                    //If there was an error routing the order, a new attempt will be made next time
                }
                else if (pos.Side == Side.Sell)
                {
                    Order order = BuildOrder(pos, Side.Sell, 0);
                    pos.Orders.Add(order);

                    DoLog(string.Format("Creating sell order for symbol {0}.Quantity={1} Price={2}",
                                        pos.Security.Symbol,
                                        order.OrderQty.HasValue ? order.OrderQty.Value : 0,
                                        order.Price.HasValue ? order.Price.Value.ToString("##.##") : "<market>"),
                            Constants.MessageType.Information);

                    NewOrderWrapper wrapper = new NewOrderWrapper(order, Config);
                    CMState state = OrderProxy.ProcessMessage(wrapper);
                    if (state.Success)
                    {
                        pos.NewDomFlag = false;
                        pos.NewPosition = false;
                        pos.PositionCanceledOrRejected = false;
                        pos.PositionCleared = false;
                        pos.PosStatus = PositionStatus.PendingNew;
                    }
                    //If there was an error routing the order, a new attempt will be made next time
                }
            }
            else throw new Exception("Invalid position side for Symbol " + pos.Security.Symbol);
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        private void ProcessExecutionReport(Wrapper wrapper)
        {
            lock (tLockCalculus)
            {
                OnMessageRcv(wrapper);
            }

        }

        #endregion

        #region Thread Methods

        public void RunOnPositionCalculus(object param)
        {
            Wrapper positionWrapper = null;
            if (param is Wrapper)
                positionWrapper = (Wrapper)param;

            if (ORConfiguration == null)
                return;

            try
            {
                Position currentPos = PositionConverter.GetPosition(positionWrapper, Config);
                try
                {
                    if (currentPos != null)
                    {
                        Thread.Sleep(ORConfiguration.OrderUpdateInMilliseconds);
                        RunNewPosition(currentPos);
                    }
                    else
                        DoLog(string.Format("Could not convert position "), Constants.MessageType.Error);

                }
                catch (Exception ex)
                {
                    DoLog(string.Format("Error error processing RunOnPositionCalculus for symbol {0}:{1}",
                            currentPos.Security.Symbol,
                            ex.Message), Constants.MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing RunOnPositionCalculus :{0}", ex.Message), Constants.MessageType.Error);
            }
        }

        #endregion

        #region Public Methods

        public CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.NEW_POSITION)
                {
                    DoLog(string.Format("Routing to market position for symbol {0}", wrapper.GetField(PositionFields.Symbol).ToString()), Constants.MessageType.Information);
                    RunOnPositionCalculus(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.CANCEL_POSITION)
                {
                    DoLog(string.Format("There is no cancel for market orders for symbol {0}", wrapper.GetField(PositionFields.Symbol).ToString()), Constants.MessageType.Information);
                    return CMState.BuildSuccess();
                }
                else
                {
                    DoLog(string.Format("Routing to market: Order Router not prepared for routing message {0}", wrapper.GetAction().ToString()), Constants.MessageType.Information);
                    return CMState.BuildFail(new Exception(string.Format("Routing to market: Order Router not prepared for routing message {0}", wrapper.GetAction().ToString())));
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing message on order router for action {0}. Error: {1}", wrapper.GetAction().ToString(), ex.Message), Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLockCalculus = new object();


                    DoLog("Initializing Order Router Proxy " + ORConfiguration.Proxy, Constants.MessageType.Information);
                    if (!string.IsNullOrEmpty(ORConfiguration.Proxy))
                    {
                        var orderProxyType = Type.GetType(ORConfiguration.Proxy);
                        if (orderProxyType != null)
                        {
                            OrderProxy = (ICommunicationModule)Activator.CreateInstance(orderProxyType);
                            OrderProxy.Initialize(ProcessOutgoing, pOnLogMsg, ORConfiguration.ProxyConfigFile);
                        }
                        else
                            throw new Exception("assembly not found: " + ORConfiguration.Proxy);
                    }
                    else
                        DoLog("Order Router proxy not found. It will not be initialized", Constants.MessageType.Error);

                    Positions = new List<Position>();
                    PositionConverter = new PositionConverter();
                    MarketDataConverter = new MarketDataConverter();
                    ExecutionReportConverter = new ExecutionReportConverter();

                    return true;
                }
                else
                {
                    DoLog("Error initializing config file " + configFile, Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }

        protected override CMState ProcessIncoming(Wrapper wrapper)
        {
            //Este módulo no tiene un modulo de incoming
            return CMState.BuildFail(new Exception("No incoming module set!"));
        }

        //Utilizado para procesar mensajes provenientes del módulo de ruteo
        protected override CMState ProcessOutgoing(Wrapper wrapper)
        {
            try
            {
                if (wrapper != null)
                {
                    DoLog("Incoming message from order routing proxy: " + wrapper.ToString(), Constants.MessageType.Information);

                    if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                    {
                        ProcessExecutionReport(wrapper);
                    }
                }

                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {

                DoLog("Error processing message from order routing: " + (wrapper != null ? wrapper.ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        #endregion
    }
}

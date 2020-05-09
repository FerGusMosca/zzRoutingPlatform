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
    public class OrderRouter : OrderRouterBase, ICommunicationModule
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

        public Thread RunOnPositionCalculusThread { get; set; }

        #endregion

        #region Private Methods

        protected virtual void EvalUpdatingPositionOnDOM(Position pos)
        {
            if (pos.NewDomFlag && !pos.NewPosition && !pos.PositionCanceledOrRejected && !pos.PositionCleared)
            {
                Order oldOrder = pos.GetCurrentOrder();
                if (oldOrder != null)
                {
                    Order order = oldOrder.Clone();
                    order.ClOrdId = pos.GetNextClOrdId(order.Index + 1);
                    pos.Orders.Add(order);
                    order.Index++;

                    if (pos.Side == Side.Buy)
                        order.Price = pos.Security.MarketData.BestBidPrice;
                    else if (pos.Side == Side.Sell)
                        order.Price = pos.Security.MarketData.BestAskPrice;
                    else
                        throw new Exception("Invalid position side for Symbol " + pos.Security.Symbol);

                    if (pos.IsNonMonetaryQuantity())
                        order.OrderQty = pos.LeavesQty;
                    else if (pos.IsMonetaryQuantity())
                    {
                        double qty = Math.Floor(pos.CashQty.Value / order.Price.Value);
                        order.OrderQty = qty - pos.CumQty;//Lo que hay que comprar menos lo ya comprado
                    }
                    else
                        throw new Exception("Could not process position quantity type: " + pos.QuantityType.ToString());

                    if (!order.OrderQty.HasValue || order.OrderQty >= 0)
                    {
                        DoLog(string.Format("@Order Router: Updating order for symbol {0}: qty={1} price={2}", pos.Symbol,order.OrderQty,order.Price), Main.Common.Util.Constants.MessageType.Information);
                        UpdateOrderWrapper wrapper = new UpdateOrderWrapper(order, Config);
                        OrderProxy.ProcessMessage(wrapper);
                    }
                    else
                    {
                        DoLog(string.Format("@Order Router: Cancelling order for symbol {0}", pos.Symbol), Main.Common.Util.Constants.MessageType.Information);
                        CancelOrderWrapper wrapper = new CancelOrderWrapper(order, Config);
                        OrderProxy.ProcessMessage(wrapper);
                    }

                    pos.NewDomFlag = false;
                }
                else
                    pos.PositionCleared = true;
            }

        }

        protected virtual Order BuildOrder(Position pos,Side side,int index)
        {
            Order order = new Order()
            {
                Security = pos.Security,
                ClOrdId = pos.GetNextClOrdId(index + (ORConfiguration.OrderIdStart.HasValue ? ORConfiguration.OrderIdStart.Value : 0)),
                Side = side,
                OrdType = OrdType.Limit,
                Price = side == Side.Buy ? pos.Security.MarketData.BestBidPrice : pos.Security.MarketData.BestAskPrice,
                TimeInForce = TimeInForce.Day,
                Currency = pos.Security.Currency,
                QuantityType = pos.QuantityType,
                PriceType = PriceType.FixedAmount,
                Account=pos.AccountId,
                Index = index
            };
            order.OrigClOrdId = order.ClOrdId;

            if (pos.IsMonetaryQuantity())
            {
                if (pos.IsSinlgeUnitSecurity())
                {
                    double qty = Math.Floor(pos.CashQty.Value / order.Price.Value);
                    order.OrderQty = qty;
                    pos.LeavesQty = qty;//Position Missing to fill in shares
                }
                else
                {
                    double qty = Math.Round(pos.CashQty.Value / order.Price.Value, 4);
                    order.OrderQty = qty;
                    pos.LeavesQty = qty;//Position Missing to fill in shares
                }
            }
            else 
            {
                if (pos.Qty.HasValue)
                {
                    order.OrderQty = pos.Qty;
                    pos.LeavesQty = pos.Qty;//Position Missing to fill in amount of shares
                }
                else if (pos.CashQty.HasValue)
                {
                    order.OrderQty = pos.CashQty;
                    pos.LeavesQty = pos.CashQty;//Position Missing to fill in amount of shares
                }
                else
                    throw new Exception("Could not process position quantity type: " + pos.QuantityType.ToString());
            }


            return order;
        }

        private void RunNewPosition(Position pos)
        {
            if (pos.NewPosition)
            {
                if (pos.Side == Side.Buy)
                {
                    if (pos.Security.MarketData.BestBidPrice.HasValue)
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
                    else
                    {
                        //InvalidNewPositionWrapper wrapper = new InvalidNewPositionWrapper(pos, PositionRejectReason.NoBidAvailable,
                        //                                                                 string.Format("Could not create order por symbol {0} because there was not best bid price as a reference", pos.Security.Symbol), Config);
                        ////The strategy handler might want to now that an order could not be created. 
                        ////But it has to know that if there was no asks availble the order router will continue trying to route the order
                        //OnMessageRcv(wrapper);
                        DoLog(string.Format("Could not create order por symbol {0} because there was not best bid price as a reference", pos.Security.Symbol),
                                             Constants.MessageType.Information);
                    }
                }
                else if (pos.Side == Side.Sell)
                {
                    if (pos.Security.MarketData.BestAskPrice.HasValue)
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
                    else
                    {
                        //InvalidNewPositionWrapper wrapper = new InvalidNewPositionWrapper(pos, PositionRejectReason.NoAskAvailable,
                        //                                                                 string.Format("Could not create order por symbol {0} because there was not best ask price as a reference", pos.Security.Symbol), Config);
                        ////The strategy handler might want to now that an order could not be created. 
                        ////But it has to know that if there was no asks availble the order router will continue trying to route the order
                        //OnMessageRcv(wrapper);
                        DoLog(string.Format("Could not create order por symbol {0} because there was not best ask price as a reference", pos.Security.Symbol),
                                             Constants.MessageType.Information);
                    }
                }
                else throw new Exception("Invalid position side for Symbol " + pos.Security.Symbol);

                
            }
        }

        protected virtual void ProcessMarketData(Wrapper wrapper)
        {

            lock (tLockCalculus)
            {
                string symbol = wrapper.GetField(MarketDataFields.Symbol).ToString();
                Position pos = Positions.Where(x => x.Security.Symbol == symbol).FirstOrDefault();

                if (pos != null && !pos.PositionCleared && ! pos.PositionCanceledOrRejected)
                {
                    MarketData updMarketData = MarketDataConverter.GetMarketData(wrapper, Config);

                    if (pos.Side == Side.Buy)
                    {
                        if (pos.Security.MarketData.BestBidPrice.HasValue && !updMarketData.BestBidPrice.HasValue)
                            pos.NewDomFlag = true;
                        else if (!pos.Security.MarketData.BestBidPrice.HasValue && updMarketData.BestBidPrice.HasValue)
                            pos.NewDomFlag = true;
                        else if (pos.Security.MarketData.BestBidPrice.HasValue && updMarketData.BestBidPrice.HasValue)
                        {

                            if (updMarketData.BestBidPrice.Value != pos.Security.MarketData.BestBidPrice.Value)
                            {
                                DoLog(string.Format("Updating DOM price on BID. Symbol: {0} - New Bid Price:{1} Old Bid Price:{2}",
                                                      pos.Security.Symbol,
                                                      pos.Security.MarketData.BestBidPrice.Value,
                                                      updMarketData.BestBidPrice.Value), Constants.MessageType.Information);
                                pos.NewDomFlag = true;
                            }
                        }
                    }

                    if (pos.Side == Side.Sell)
                    {

                        if (pos.Security.MarketData.BestAskPrice.HasValue && !updMarketData.BestAskPrice.HasValue)
                            pos.NewDomFlag = true;
                        else if (!pos.Security.MarketData.BestAskPrice.HasValue && updMarketData.BestAskPrice.HasValue)
                            pos.NewDomFlag = true;
                        else if (pos.Security.MarketData.BestAskPrice.HasValue && updMarketData.BestAskPrice.HasValue)
                        {

                            if (updMarketData.BestAskPrice.Value != pos.Security.MarketData.BestAskPrice.Value)
                            {
                                DoLog(string.Format("Updating DOM price on ASK. Symbol: {0} - New Ask Price:{1} Old Ask Price:{2}",
                                                      pos.Security.Symbol,
                                                      pos.Security.MarketData.BestAskPrice.Value,
                                                      updMarketData.BestAskPrice.Value), Constants.MessageType.Information);
                                pos.NewDomFlag = true;
                            }
                        }
                    }

                    pos.Security.MarketData = updMarketData;
                }
            }
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected virtual void ProcessExecutionReport(Wrapper wrapper)
        {
            lock (tLockCalculus)
            {
                string symbol = wrapper.GetField(ExecutionReportFields.Symbol).ToString();
                Position pos = Positions.Where(x => x.Security.Symbol == symbol).FirstOrDefault();

                if (pos != null)
                {
                    ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                    if (report != null && report.Order != null && pos.GetCurrentOrder() != null)
                        pos.GetCurrentOrder().OrderId = report.Order.OrderId;
                        

                    if (report != null)
                    {
                        //Partially Filled
                        if (report.ExecType == ExecType.Trade && report.OrdStatus == OrdStatus.PartiallyFilled)
                        {
                            pos.CumQty = report.CumQty;
                            pos.LeavesQty = report.LeavesQty;
                            pos.AvgPx = report.AvgPx;
                            pos.LastMkt = report.LastMkt;
                            pos.LastPx = report.LastPx;
                            pos.LastQty = report.LastQty;
                            pos.PositionCleared = false;
                            pos.SetPositionStatusFromExecution(report.ExecType);
                            pos.ExecutionReports.Add(report);

                            OnMessageRcv(wrapper);

                        }//Filled
                        else if (report.ExecType == ExecType.Trade && report.OrdStatus == OrdStatus.Filled)
                        {
                            pos.CumQty = report.CumQty;
                            pos.LeavesQty = report.LeavesQty;
                            pos.AvgPx = report.AvgPx;
                            pos.LastMkt = report.LastMkt;
                            pos.LastPx = report.LastPx;
                            pos.LastQty = report.LastQty;
                            pos.PositionCleared = true;
                            pos.SetPositionStatusFromExecution(report.ExecType);
                            pos.ExecutionReports.Add(report);

                            Positions.Remove(pos);

                            OnMessageRcv(wrapper);
                        }
                        else if (report.ExecType == ExecType.DoneForDay || report.ExecType == ExecType.Stopped
                                 || report.ExecType == ExecType.Suspended || report.ExecType == ExecType.Rejected
                                 || report.ExecType == ExecType.Expired || report.ExecType == ExecType.Canceled)
                        {
                            pos.PositionCanceledOrRejected = true;
                            pos.PositionCleared = false;
                            pos.ExecutionReports.Add(report);
                            pos.SetPositionStatusFromExecution(report.ExecType);
                            Positions.Remove(pos);
                            OnMessageRcv(wrapper);
                        }
                        else
                        {
                            pos.SetPositionStatusFromExecution(report.ExecType);
                            pos.ExecutionReports.Add(report);
                            OnMessageRcv(wrapper);
                        }
                    }
                }
            }
        
        }

        protected void CancelOrder(Wrapper wrapper)
        {
            if (wrapper.GetField(PositionFields.PosId) != null)
            {
                string posId = Convert.ToString(wrapper.GetField(PositionFields.PosId));

                Position posInOrderRouter = Positions.Where(x => x.PosId == posId).FirstOrDefault();

                if(posInOrderRouter!=null)
                {
                    if ( !posInOrderRouter.PositionCleared && !posInOrderRouter.PositionCanceledOrRejected && !posInOrderRouter.NewPosition)
                    {
                        Order order = posInOrderRouter.GetCurrentOrder();

                        if (order != null)
                        {
                            DoLog(string.Format("@GenericOrderRouter: Cancelling Order Id {0} Symbol={1}  Side={4} Qty={2} Price={3}}", 
                                                order.OrderId, order.Symbol,order.OrderQty, order.Price.HasValue ? order.Price.Value.ToString() : "<mkt>", 
                                                order.Side), Main.Common.Util.Constants.MessageType.Information);

                            CancelOrderWrapper cancelOrderWrapper = new CancelOrderWrapper(order, Config);

                            OrderProxy.ProcessMessage(cancelOrderWrapper);
                        }
                        else
                            throw new Exception(string.Format("Could not cancel order for symbol {0} because no orders where found!", posInOrderRouter.Symbol));
                    }
                    else
                        throw new Exception(string.Format("Could not cancel order for position for symbol {0} because it is in an invalid state: {1}",posInOrderRouter.Symbol,posInOrderRouter.PosStatus));
                }
                else
                    throw new Exception(string.Format("Could not cancel order for unknown position {0}", posId));
                
            }
            else
                throw new Exception(string.Format("Could not cancel order if no PosId was specified"));
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
                bool run = true;
                Position currentPos = null;
                lock (tLockCalculus)
                {
                    currentPos = PositionConverter.GetPosition(positionWrapper, Config);
                    Positions.Add(currentPos);
                }

                while (run)
                {
                    try
                    {
                        Position posInOrderRouter = null;

                        lock (tLockCalculus)
                        {
                            posInOrderRouter = Positions.Where(x => x.PosId == currentPos.PosId).FirstOrDefault();
                        }

                        if (posInOrderRouter!=null && !posInOrderRouter.PositionCleared && !posInOrderRouter.PositionCanceledOrRejected)
                        {
                            lock (tLockCalculus)
                            {
                                if (currentPos.NewPosition)
                                {
                                    RunNewPosition(currentPos);
                                }
                                else if (!currentPos.NewPosition && currentPos.NewDomFlag)//Solo si la posición tiene un nuevo DOM
                                {
                                    EvalUpdatingPositionOnDOM(currentPos);
                                }
                            }
                        }
                        else
                        {
                            run = false;
                            DoLog(string.Format("Ending RunOnPositionCalculus for symbol {0}. The position is cleared",
                                currentPos.Security.Symbol), Constants.MessageType.Error);
                        }

                        Thread.Sleep(ORConfiguration.OrderUpdateInMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("Error error processing RunOnPositionCalculus for symbol {0}:{1}",
                                currentPos.Security.Symbol,
                                ex.Message), Constants.MessageType.Error);
                    }
                }

               
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical error processing RunOnPositionCalculus :{0}",ex.Message), Constants.MessageType.Error);
            }
        }

        #endregion

        #region Public Methods

        public virtual CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.NEW_POSITION)
                {
                    string symbol = Convert.ToString(wrapper.GetField(PositionFields.Symbol));
                    if (!Positions.Any(x => x.Symbol == symbol))
                    {
                        DoLog(string.Format("Routing to market position for symbol {0}", wrapper.GetField(PositionFields.Symbol).ToString()), Constants.MessageType.Information);
                        RunOnPositionCalculusThread = new Thread(new ParameterizedThreadStart(RunOnPositionCalculus));
                        RunOnPositionCalculusThread.Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else
                        return CMState.BuildFail(new Exception(string.Format("There is already an position being processed for symbol {0}", symbol)));
                }
                else if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    //DoLog(string.Format("Receiving Market Data on order router: {0}",wrapper.ToString()), Constants.MessageType.Information);
                    ProcessMarketData(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.CANCEL_POSITION)
                {
                    DoLog(string.Format("Cancelling order for symbol {0}",wrapper.GetField(PositionFields.Symbol).ToString()), Constants.MessageType.Information);
                    CancelOrder(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ALL_POSITIONS)
                {
                    DoLog(string.Format("Canceling all active orders"), Constants.MessageType.Information);
                    OrderProxy.ProcessMessage(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.SECURITY_LIST)
                {
                    OrderProxy.ProcessMessage(wrapper);
                    DoLog(string.Format("Routing security list to order router"), Constants.MessageType.Information);
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
                DoLog(string.Format("Error processing message on order router for action {0}. Error: {1}" , wrapper.GetAction().ToString(),ex.Message), Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public virtual bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLockCalculus = new object();

                    Positions = new List<Position>();
                    PositionConverter = new PositionConverter();
                    MarketDataConverter = new MarketDataConverter();
                    ExecutionReportConverter = new ExecutionReportConverter();


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
                    DoLog("@Generic Order Router: Incoming message from order routing proxy: " + wrapper.ToString(), Constants.MessageType.Debug);

                    if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                    {
                        ProcessExecutionReport(wrapper);
                    }
                    else
                    {
                        OnMessageRcv(wrapper);
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

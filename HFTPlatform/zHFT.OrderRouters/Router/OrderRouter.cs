using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
            get { return (Common.Configuration.Configuration) Config; }
            set { Config = value; }
        }

        public static object tLockCalculus { get; set; }

        public Dictionary<string, Position> Positions { get; set; }

        public Dictionary<string, Position> PositionsByClOrId { get; set; }

        public Dictionary<string, DateTime> PositionsTimeoutDict { get; set; }

        public Thread RunOnPositionCalculusThread { get; set; }

        #endregion

        #region Private Methods

        private void UpdateTimeoutPosDict(string posId)
        {
            lock (PositionsTimeoutDict)
            {
                if (!PositionsTimeoutDict.ContainsKey(posId))
                    PositionsTimeoutDict.Add(posId, DateTime.Now);
                else
                    PositionsTimeoutDict[posId] = DateTime.Now;
            }
        }

        private void CleanTimeoutPosDict(string posId)
        {
            lock (PositionsTimeoutDict)
            {
                if(PositionsTimeoutDict.ContainsKey(posId))
                    PositionsTimeoutDict.Remove(posId);

            }
        }

        protected void OrdersTimeoutThread(object param)
        {

            try
            {
                while (true)
                {
                    lock (PositionsTimeoutDict)
                    {
                        foreach (string posId in PositionsTimeoutDict.Keys)
                        {
                            TimeSpan elapsed = DateTime.Now - PositionsTimeoutDict[posId];

                            if (elapsed.TotalSeconds > 20)//20 seconds
                            {
                                //Remove current order
                                if (Positions.ContainsKey(posId) )
                                {
                                    Position position = Positions[posId];
                                    if (position.PendingCxlRepl)
                                    {
                                        position.RemoveLastOrder();
                                        position.PendingCxlRepl = false;
                                    }
                                }
                            }
                        }

                    }
                    Thread.Sleep(100);
                }
            }
            catch(Exception ex)
            {
                DoLog($"CRITICAL ERROR with timeout threads at Generic Order Router:{ex.Message}", Constants.MessageType.Error);
            }
        
        }

        protected virtual void EvalUpdPosOnNewMarketData(Position pos)
        {
            if (pos.NewDomFlag && 
                !pos.NewPosition && 
                !pos.PositionCanceledOrRejected && 
                !pos.PositionCleared &&
                !pos.PendingCxlRepl)
            {

                pos.PendingCxlRepl = true;
                UpdateTimeoutPosDict(pos.PosId);
                Order oldOrder = pos.GetCurrentOrder();
                if (oldOrder != null)
                {
                    Order order = oldOrder.Clone();
                    //string origClOrdId = order.OrigClOrdId;
                    order.ClOrdId = pos.GetNextClOrdId(order.Index + 1);
                    //order.OrigClOrdId = origClOrdId;
                    pos.Orders.Add(order);
                    order.Index++;
                    DoLog(string.Format("<Gen. Order Router> - Replacing OrigClOrdId {0} with ClOrdid {1} for symbol {2} (PosId {3}) ",order.OrigClOrdId,order.ClOrdId,pos.Security.Symbol,pos.PosId),Constants.MessageType.Information);
                    PositionsByClOrId.Add(order.ClOrdId, pos);

                    if (pos.Side == Side.Buy)
                    {
                        if (pos.Security.MarketData.BestBidPrice.HasValue)
                            order.Price = pos.Security.MarketData.BestBidPrice;
                        else
                            DoLog(
                                string.Format(
                                    "Waiting to send order for security {0} (PosId={1}) because there is not a bid price as a reference",
                                    order.Security.Symbol, pos.PosId), Constants.MessageType.Information);
                    }
                    else if (pos.Side == Side.Sell)
                    {
                        if (pos.Security.MarketData.BestAskPrice.HasValue)
                            order.Price = pos.Security.MarketData.BestAskPrice;
                        else
                            DoLog(
                                string.Format("Waiting to send order for security {0} (PosId={1}) because there is not an ask price as a reference",order.Security.Symbol, pos.PosId), Constants.MessageType.Information);
                    }
                    else
                        throw new Exception("Invalid position side for Symbol " + pos.Security.Symbol);

                    if (pos.IsNonMonetaryQuantity())
                        order.OrderQty = pos.LeavesQty;
                    else if (pos.IsMonetaryQuantity())
                    {
                        order.OrderQty = pos.LeavesQty;
                        //double qty = Math.Floor(pos.CashQty.Value / order.Price.Value);
                        //order.OrderQty = qty - pos.CumQty; //Lo que hay que comprar menos lo ya comprado
                    }
                    else
                        throw new Exception("Could not process position quantity type: " + pos.QuantityType.ToString());

                    if (!order.OrderQty.HasValue || order.OrderQty >= 0)
                    {
                        DoLog(string.Format( "@Order Router: Updating order for symbol {0} (PosId={3}) : qty={1} price={2}",pos.Symbol, order.OrderQty, order.Price, pos.PosId),Main.Common.Util.Constants.MessageType.Information);
                        UpdateOrderWrapper wrapper = new UpdateOrderWrapper(order, Config);
                        OrderProxy.ProcessMessage(wrapper);
                    }
                    else
                    {
                        DoLog($"<DBG> CashQty={pos.CashQty} Price={order.Price.Value}", Constants.MessageType.Information);
                        DoLog(string.Format("<DBG>@Order Router: Cancelling order for symbol {0} (PosId={1})", pos.Symbol,pos.PosId), Main.Common.Util.Constants.MessageType.Information);
                        CancelOrderWrapper wrapper = new CancelOrderWrapper(order, Config);
                        OrderProxy.ProcessMessage(wrapper);
                    }

                    pos.NewDomFlag = false;
                }
                else
                    pos.PositionCleared = true;
            }
            else
            {
                DoLog(string.Format("DB-x --> No if entry For PosId={0} NewDomFlag={1} NewPosition={2} PositionCanceledOrRejected={3} PositionCleared={4}",
                    pos.PosId,pos.NewDomFlag,pos.NewPosition,pos.PositionCanceledOrRejected,pos.PositionCleared),Constants.MessageType.Information);

            }

        }

        protected virtual Order BuildOrder(Position pos, Side side, int index)
        {
            Order order = new Order()
            {
                Security = pos.Security,
                ClOrdId = pos.GetNextClOrdId(index + (ORConfiguration.OrderIdStart.HasValue
                                                 ? ORConfiguration.OrderIdStart.Value
                                                 : 0)),
                Side = side,
                OrdType = OrdType.Limit,
                Price = side == Side.Buy ? pos.Security.MarketData.BestBidPrice : pos.Security.MarketData.BestAskPrice,
                TimeInForce = TimeInForce.Day,
                Currency = pos.Security.MarketData.Currency != null
                    ? pos.Security.MarketData.Currency
                    : pos.Security.Currency,
                QuantityType = pos.QuantityType,
                PriceType = PriceType.FixedAmount,
                Account = pos.AccountId,
                Index = index
            };
            order.OrigClOrdId = order.ClOrdId;

            if (pos.IsMonetaryQuantity())
            {
                if (pos.IsSinlgeUnitSecurity())
                {
                    double qty = Math.Floor(pos.CashQty.Value / order.Price.Value);
                    order.OrderQty = qty;
                    pos.LeavesQty = qty; //Position Missing to fill in shares
                }
                else
                {
                    double qty = Math.Round(pos.CashQty.Value / order.Price.Value, 4);
                    order.OrderQty = qty;
                    pos.LeavesQty = qty; //Position Missing to fill in shares
                }
            }
            else
            {
                if (pos.Qty.HasValue)
                {
                    order.OrderQty = pos.Qty;
                    pos.LeavesQty = pos.Qty; //Position Missing to fill in amount of shares
                }
                else if (pos.CashQty.HasValue)
                {
                    order.OrderQty = pos.CashQty;
                    pos.LeavesQty = pos.CashQty; //Position Missing to fill in amount of shares
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
                        PositionsByClOrId.Add(order.ClOrdId, pos);

                        DoLog(string.Format("Creating buy order for symbol {0} (PosId={3}): Quantity={1} Price={2} ClOrdId={4}",
                                pos.Security.Symbol,
                                order.OrderQty.HasValue ? order.OrderQty.Value : 0,
                                order.Price.HasValue ? order.Price.Value.ToString("##.##") : "<market>",
                                pos.PosId,order.ClOrdId),
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
                        DoLog(
                            string.Format(
                                "Could not create order por symbol {0} (PosId={1}) because there was not best bid price as a reference",
                                pos.Security.Symbol, pos.PosId),
                            Constants.MessageType.Information);
                    }
                }
                else if (pos.Side == Side.Sell)
                {
                    if (pos.Security.MarketData.BestAskPrice.HasValue)
                    {
                        Order order = BuildOrder(pos, Side.Sell, 0);
                        pos.Orders.Add(order);
                        PositionsByClOrId.Add(order.ClOrdId, pos);

                        DoLog(string.Format("Creating sell order for symbol {0} (PosId={3}): Quantity={1} Price={2} ClOrdId={4}",
                                pos.Security.Symbol,
                                order.OrderQty.HasValue ? order.OrderQty.Value : 0,
                                order.Price.HasValue ? order.Price.Value.ToString("##.##") : "<market>",
                                pos.PosId,
                                order.ClOrdId),
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
                        DoLog(
                            string.Format(
                                "Could not create order por symbol {0} (PosId={1}) because there was not best ask price as a reference",
                                pos.Security.Symbol, pos.PosId),
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

                List<Position> openPos = Positions.Values.Where(x => x.PositionRouting() && x.Security.Symbol == symbol)
                    .ToList();

                foreach (Position pos in openPos)
                {

                    if (pos != null && !pos.PositionCleared && !pos.PositionCanceledOrRejected)
                    {
                        MarketData updMarketData = MarketDataConverter.GetMarketData(wrapper, Config);

                        if (pos.Side == Side.Buy)
                        {
                            if (pos.Security.MarketData.BestBidPrice.HasValue && !updMarketData.BestBidPrice.HasValue)
                                pos.NewDomFlag = true;
                            else if (!pos.Security.MarketData.BestBidPrice.HasValue &&
                                     updMarketData.BestBidPrice.HasValue)
                                pos.NewDomFlag = true;
                            else if (pos.Security.MarketData.BestBidPrice.HasValue &&
                                     updMarketData.BestBidPrice.HasValue)
                            {

                                if (updMarketData.BestBidPrice.Value != pos.Security.MarketData.BestBidPrice.Value)
                                {
                                    DoLog(string.Format(
                                        "Updating DOM price on BID. Symbol: {0} (PosId={3}) - New Bid Price:{1} Old Bid Price:{2}",
                                        pos.Security.Symbol,
                                        pos.Security.MarketData.BestBidPrice.Value,
                                        updMarketData.BestBidPrice.Value,
                                        pos.PosId), Constants.MessageType.Information);
                                    pos.NewDomFlag = true;
                                }
                            }
                        }

                        if (pos.Side == Side.Sell)
                        {

                            if (pos.Security.MarketData.BestAskPrice.HasValue && !updMarketData.BestAskPrice.HasValue)
                                pos.NewDomFlag = true;
                            else if (!pos.Security.MarketData.BestAskPrice.HasValue &&
                                     updMarketData.BestAskPrice.HasValue)
                                pos.NewDomFlag = true;
                            else if (pos.Security.MarketData.BestAskPrice.HasValue &&
                                     updMarketData.BestAskPrice.HasValue)
                            {

                                if (updMarketData.BestAskPrice.Value != pos.Security.MarketData.BestAskPrice.Value)
                                {
                                    DoLog(string.Format(
                                        "Updating DOM price on ASK. Symbol: {0} (PosId={3}) - New Ask Price:{1} Old Ask Price:{2}",
                                        pos.Security.Symbol,
                                        pos.Security.MarketData.BestAskPrice.Value,
                                        updMarketData.BestAskPrice.Value,
                                        pos.PosId), Constants.MessageType.Information);
                                    pos.NewDomFlag = true;
                                }
                            }
                        }

                        pos.Security.MarketData = updMarketData;
                    }
                }
            }
        }

        protected override void DoLoadConfig(string configFile, List<string> listaCamposSinValor)
        {
            List<string> noValueFields = new List<string>();
            Config =
                new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(
                    configFile, noValueFields);
        }

        protected void RemovePositionOnFinishedOrder(Position pos, ExecutionReport report)
        {
            DoLog(string.Format("<Gen. Order Router> - Removing PosId {0} on {1} report",pos.PosId,report.OrdStatus),Constants.MessageType.Information);
            Positions.Remove(pos.PosId);
            
            if(report.Order.OrigClOrdId!=null && PositionsByClOrId.ContainsKey(report.Order.OrigClOrdId))
                PositionsByClOrId.Remove(report.Order.OrigClOrdId);
            else if (report.Order.ClOrdId!=null && PositionsByClOrId.ContainsKey(report.Order.ClOrdId))
                PositionsByClOrId.Remove(report.Order.ClOrdId);
            else if (pos.GetCurrentOrder().OrigClOrdId!=null && PositionsByClOrId.ContainsKey(pos.GetCurrentOrder().OrigClOrdId))
                PositionsByClOrId.Remove(pos.GetCurrentOrder().OrigClOrdId);
            else if (pos.GetCurrentOrder().ClOrdId!=null && PositionsByClOrId.ContainsKey(pos.GetCurrentOrder().ClOrdId))
                PositionsByClOrId.Remove(pos.GetCurrentOrder().ClOrdId);
            else
            {
                DoLog(string.Format("WARNING- Could not find PositionsByClOrId by OrigClOrdId/ClOrdId for PosId {0}",
                    pos.PosId),Constants.MessageType.Information);
            }
            
            DoLog(string.Format("<Gen. Order Router> - Removing PosId {0} (ClOrId {1}) on report {2}", pos.PosId,pos.GetCurrentOrder().ClOrdId,report.OrdStatus), Constants.MessageType.Information);

        }

        protected Position FindPositionInMemory(Wrapper reportWrapper)
        {
            string symbol = (string)  reportWrapper.GetField(ExecutionReportFields.Symbol);
            string clOrdid = (string) reportWrapper.GetField(ExecutionReportFields.ClOrdID);
            string origClOrdId = (string) reportWrapper.GetField(ExecutionReportFields.OrigClOrdID);
            
            Position pos = null;

            if (clOrdid != null && PositionsByClOrId.ContainsKey(clOrdid))
                pos = PositionsByClOrId[clOrdid];
            else if (origClOrdId != null && PositionsByClOrId.ContainsKey(origClOrdId))
                pos = PositionsByClOrId[origClOrdId];
            else
            {
                //Console.Beep();
                pos = Positions.Values.Where(x => x.PositionRouting() && x.Symbol==symbol).FirstOrDefault();
                if (pos!=null)
                    DoLog(string.Format("SOFTWARN- Could not find position by ClOrdId {0} or OrigClOrdId {1} but we used a routing position for symbol {2}",
                        clOrdid,origClOrdId,symbol),Constants.MessageType.Error);
                else
                {
                    DoLog(string.Format(
                        "SOFTWARN- External Trading? - Could not find position by ClOrdId {0} or OrigClOrdId {1} or symbol {2}",
                        clOrdid, origClOrdId, symbol), Constants.MessageType.Error);
                    return null;
                }
            }

            return pos;
        }

        protected virtual void ProcessExecutionReport(Wrapper wrapper)
        {
            lock (tLockCalculus)
            {
                Position pos = FindPositionInMemory(wrapper);

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
                            pos.PendingCxlRepl = false;
                            pos.SetPositionStatusFromExecution(report.ExecType);
                            pos.ExecutionReports.Add(report);
                            CleanTimeoutPosDict(pos.PosId);
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
                            pos.PendingCxlRepl = false;

                            pos.SetPositionStatusFromExecution(report.ExecType);
                            pos.ExecutionReports.Add(report);
                            RemovePositionOnFinishedOrder(pos, report);
                            CleanTimeoutPosDict(pos.PosId);
                            OnMessageRcv(wrapper);
                        }
                        else if (report.ExecType == ExecType.DoneForDay || report.ExecType == ExecType.Stopped
                                 || report.ExecType == ExecType.Suspended || report.ExecType == ExecType.Rejected
                                 || report.ExecType == ExecType.Expired || report.ExecType == ExecType.Canceled)
                        {
                            pos.PositionCanceledOrRejected = true;
                            pos.PositionCleared = false;
                            pos.PendingCxlRepl = false;
                            pos.SetPositionStatusFromExecution(report.ExecType);
                            pos.ExecutionReports.Add(report);
                            RemovePositionOnFinishedOrder(pos, report);
                            CleanTimeoutPosDict(pos.PosId);
                            OnMessageRcv(wrapper);
                        }
                        else if (report.ExecType == ExecType.New)
                        {
                            pos.PendingCxlRepl = false;
                            CleanTimeoutPosDict(pos.PosId);
                            pos.SetPositionStatusFromExecution(report.ExecType);
                            pos.ExecutionReports.Add(report);
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
                else
                {
                    string clOrdid = (string) wrapper.GetField(ExecutionReportFields.ClOrdID);
                    DoLog(string.Format("<Generic Order Router> - External Trading for ClOrdId {0}",
                        clOrdid != null ? clOrdid : "NO ClOrId"), Constants.MessageType.Information);
                    //OnMessageRcv(wrapper);//External Trading
                    //Better not send this as it might be a lost ER than might re open a position!
                }
            }
        
        }

        protected void CancelOrder(Wrapper wrapper)
        {
            if (wrapper.GetField(PositionFields.PosId) != null)
            {
                string posId = Convert.ToString(wrapper.GetField(PositionFields.PosId));

                if (Positions.ContainsKey(posId))
                {
                    Position posInOrderRouter = Positions[posId];

                    if (posInOrderRouter != null)
                    {
                        //It is not this module responsability to validate the Position Status if Cancellation is requested
                        Order order = posInOrderRouter.GetCurrentOrder();

                        if (order != null)
                        {
                            DoLog(string.Format(
                                "<<Gen. Order Router> - Cancelling Order Id {0} Symbol={1}  Side={4} Qty={2} Price={3} (PosId={4})",
                                order.OrderId, order.Symbol, order.OrderQty,
                                order.Price.HasValue ? order.Price.Value.ToString() : "<mkt>",
                                order.Side, posId), Main.Common.Util.Constants.MessageType.Information);

                            CancelOrderWrapper cancelOrderWrapper = new CancelOrderWrapper(order, Config);

                            OrderProxy.ProcessMessage(cancelOrderWrapper);
                        }
                        else
                            throw new Exception(string.Format(
                                "ERROR-Could not cancel order for symbol {0} (PosId={1}) because no orders where found!",
                                posInOrderRouter.Symbol, posInOrderRouter.PosId));
                    }
                    else
                        throw new Exception(string.Format("ERROR-Could not cancel order for unknown position {0}",
                            posId));
                }
                else
                {
                    DoLog(string.Format("ERROR-Could not find a position for posId {0} to cancel",posId),Constants.MessageType.Error);
                }
            }
            else
                throw new Exception(string.Format("ERROR-Could not cancel order if no PosId was specified"));
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
                    if (!Positions.ContainsKey(currentPos.PosId))
                    {
                        DoLog(String.Format("<Gen. Order Router> - Adding new position with PosId {0} for symbol {1}",currentPos.PosId,currentPos.Security.Symbol),Constants.MessageType.Information);
                        Positions.Add(currentPos.PosId, currentPos);
                    }
                    else
                    {
                        throw new Exception(String.Format("Position with PosId {0} for symbol {1} has already been added!",currentPos.PosId,currentPos.Security.Symbol));
                    }
                    
                }

                while (run)
                {
                    try
                    {
                        Position posInOrderRouter = null;

                        lock (tLockCalculus)
                        {
                            if(Positions.ContainsKey(currentPos.PosId))
                                posInOrderRouter = Positions[currentPos.PosId];
                            else
                            {
                                DoLog(string.Format("<Gen. Order Router> - Position Id {0} no longer active...",currentPos.PosId),Constants.MessageType.Information);
                                return;
                            }
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
                                    EvalUpdPosOnNewMarketData(currentPos);
                                }
                            }
                        }
                        else
                        {
                            run = false;
                            DoLog(string.Format("ERROR-Ending RunOnPositionCalculus for symbol {0} (PosId={1}). The position is cleared",
                                currentPos.Security.Symbol,currentPos.PosId), Constants.MessageType.Error);
                        }

                        Thread.Sleep(ORConfiguration.OrderUpdateInMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        DoLog(string.Format("<Gen. Order Router> - ERROR-Error processing RunOnPositionCalculus for symbol {0} (PosId={2}):{1}-{3}",
                                currentPos.Security.Symbol,
                                ex.Message,currentPos.PosId,ex.StackTrace), Constants.MessageType.Error);
                    }
                }

               
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Critical ERROR processing RunOnPositionCalculus :{0}",ex.Message), Constants.MessageType.Error);
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
                    string posId = Convert.ToString(wrapper.GetField(PositionFields.PosId));
                    if (!Positions.ContainsKey(posId))
                    {
                        DoLog(string.Format("<Gen. Order Router.> - Routing to market position for symbol {0} (PosId={1})",symbol,posId), Constants.MessageType.Information);
                        RunOnPositionCalculusThread = new Thread(new ParameterizedThreadStart(RunOnPositionCalculus));
                        RunOnPositionCalculusThread.Start(wrapper);
                        return CMState.BuildSuccess();
                    }
                    else
                        return CMState.BuildFail(new Exception(string.Format("There is already a position being processed for symbol {0} (PosId={1})", symbol,posId)));
                }
                else if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    //DoLog(string.Format("Receiving Market Data on order router: {0}",wrapper.ToString()), Constants.MessageType.Information);
                    ProcessMarketData(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.CANCEL_POSITION)
                {
                    string posId = (string) wrapper.GetField(PositionFields.PosId);
                    string symbol = (string) wrapper.GetField(PositionFields.Symbol);
                    DoLog(string.Format("Cancelling order for symbol {0} (PosId={1})",symbol,posId), Constants.MessageType.Information);
                    CancelOrder(wrapper);
                    return CMState.BuildSuccess();
                }
                else if (wrapper.GetAction() == Actions.CANCEL_ORDER)
                {
                    OrderProxy.ProcessMessage(wrapper);
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
                else if (wrapper.GetAction() == Actions.ORDER_MASS_STATUS_REQUEST)
                {
                    OrderProxy.ProcessMessage(wrapper);
                    DoLog(string.Format("Routing order mass status request to order router"), Constants.MessageType.Information);
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
                DoLog(string.Format("ERROR processing message on order router for action {0}. Error: {1}" , wrapper.GetAction().ToString(),ex.Message), Constants.MessageType.Error);
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

                    Positions = new Dictionary<string, Position>();
                    PositionsByClOrId = new Dictionary<string, Position>();
                    PositionsTimeoutDict = new Dictionary<string, DateTime>();
                    PositionConverter = new PositionConverter();
                    MarketDataConverter = new MarketDataConverter();
                    ExecutionReportConverter = new ExecutionReportConverter();
                    


                    DoLog("Initializing Generic Order Router " + ORConfiguration.Proxy, Constants.MessageType.Information);
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
                        DoLog("Generic Order Router not found. It will not be initialized", Constants.MessageType.Error);

                    Thread myTimeoutThreads = new Thread(OrdersTimeoutThread);
                    myTimeoutThreads.Start();

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
                    //DoLog("@Generic Order Router: Incoming message from Real Order Router: " + wrapper.ToString(), Constants.MessageType.Debug);

                    if (wrapper.GetAction() == Actions.EXECUTION_REPORT)
                    {
                        //DoLog("@Generic Order Router: Incoming execution report from Real -Order Router", Constants.MessageType.Debug);

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

                DoLog("<Generic Order Router> ERROR @ProcessOutgoing: " + (wrapper != null ? wrapper.GetAction().ToString() : "") + " Error:" + ex.Message, Constants.MessageType.Error);

                return CMState.BuildFail(ex);
            }
        }

        #endregion
    }
}

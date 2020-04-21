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
    public class SoftMarketOrderRouter : OrderRouter
    {
        #region Protected Attributes

        protected ICommunicationModule MarketDataModule { get; set; }

        public Dictionary<string,Position> PositionsDict { get; set; }

        public Dictionary<string, Position> PendingPartialOrderPositionsDict { get; set; }

        public Dictionary<string, Position> ActiveOrdersDict { get; set; }

        public static object tLock { get; set; }

        #endregion

        #region Private Methods

        private void LogNewOrder(Position pos, Order newOrder)
        {
            DoLog(string.Format("Creating buy order for symbol {0}.Quantity={1} Price={2}",
                                                pos.Security.Symbol,
                                                newOrder.OrderQty.HasValue ? newOrder.OrderQty.Value : 0,
                                                newOrder.Price.HasValue ? newOrder.Price.Value.ToString("##.##") : "<market>"),
                                                Constants.MessageType.Information);
        }

        protected double GetFullOrderQty(Position pos, double price)
        {
            if (!pos.IsSinlgeUnitSecurity())
                throw new Exception(string.Format("Not implemented quantity conversion for seurity type {0}", pos.Security.SecType.ToString()));

            if (pos.CumQty == 0)
            {
                if (pos.IsMonetaryQuantity())
                {
                    pos.Qty = Convert.ToInt64(Math.Floor(pos.CashQty.Value / price));
                    pos.CashQty = null;//this doesn't apply anymore
                    return Convert.ToDouble(pos.Qty);
                }
                else
                {
                    if (pos.Qty.HasValue)
                        return Convert.ToDouble(pos.Qty.Value);
                    else
                        throw new Exception(string.Format("Missing quantity for new order for security {0}", pos.Security.Symbol));
                }
            }
            else
            { 
                //We had some traeds, now we use the LeavesQty
                return Convert.ToDouble(pos.LeavesQty);
            }
        }

        protected double GetQty(Position pos, MarketData md)
        {
            
            if (pos.Side == Side.Buy)
            {
                if (!md.BestAskPrice.HasValue)
                    throw new Exception(string.Format("There is not an ask price for security {0}", pos.Security.Symbol));

                double fullLvsQty = GetFullOrderQty(pos, md.BestAskPrice.Value);
                if (md.BestAskSize.HasValue)
                {
                    if (md.BestAskSize.Value > fullLvsQty)
                        return Convert.ToDouble(fullLvsQty);
                    else
                        return md.BestAskSize.Value;
                }
                else if (md.BestAskCashSize.HasValue)
                {
                    if (md.BestAskCashSize.Value > Convert.ToDecimal(fullLvsQty))
                        return fullLvsQty;
                    else
                        return Convert.ToDouble(md.BestAskCashSize.Value);
                }
                else
                    throw new Exception(string.Format("Could not find an ask qty for security {0}", pos.Security.Symbol));
            }
            else if (pos.Side == Side.Sell)
            {
                if (!md.BestBidPrice.HasValue)
                    throw new Exception(string.Format("There is not a bid price for security {0}", pos.Security.Symbol));

                double fullLvsQty = GetFullOrderQty(pos, md.BestBidPrice.Value);

                if (md.BestBidSize.HasValue)
                {
                    if (md.BestBidSize.Value > fullLvsQty)
                        return fullLvsQty;
                    else
                        return md.BestBidSize.Value;
                }
                else if (md.BestBidCashSize.HasValue)
                {
                    if (md.BestBidCashSize.Value > Convert.ToDecimal(fullLvsQty))
                        return fullLvsQty;
                    else
                        return Convert.ToDouble(md.BestBidCashSize.Value);
                }
                else
                    throw new Exception(string.Format("Could not find a bid qty for security {0}", pos.Security.Symbol));
            }
            else
                throw new Exception(string.Format("Could not process side {0} for a new order for symbol", pos.Side.ToString(), pos.Security.Symbol));
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            Config = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        protected virtual Order BuildOrder(Position pos, int index, double qty)
        {
            string clOrdId = Guid.NewGuid().ToString();
            Order order = new Order()
            {
                Security = pos.Security,
                ClOrdId = clOrdId,
                OrigClOrdId = clOrdId,
                Side = pos.Side,
                OrdType = OrdType.Market,
                TimeInForce = TimeInForce.Day,
                Currency = pos.Security.Currency,
                QuantityType = pos.QuantityType,
                PriceType = PriceType.FixedAmount,
                Account = pos.AccountId,
                Index = index,// this index is the order number in the fina order list
                OrderQty = qty
            };
            
            pos.LeavesQty = qty;

            return order;
        }

        protected override void ProcessMarketData(Wrapper wrapper)
        {
            lock (tLock)
            {
                MarketData updMarketData = MarketDataConverter.GetMarketData(wrapper, Config);

                foreach (Position pos in PositionsDict.Values.Where(x =>   x.Security.Symbol == updMarketData.Security.Symbol 
                                                                        && x.PositionRouting() ))
                {
                    if (!PendingPartialOrderPositionsDict.ContainsKey(pos.PosId))
                    {
                        double qty = 0;
                        try
                        {
                            qty = GetQty(pos, updMarketData);
                            Order newOrder = BuildOrder(pos, pos.Orders.Count, qty);
                            pos.Orders.Add(newOrder);

                            LogNewOrder(pos, newOrder);

                            PendingPartialOrderPositionsDict.Add(pos.PosId, pos);
                            ActiveOrdersDict.Add(newOrder.ClOrdId, pos);

                            CMState processed = OrderProxy.ProcessMessage(new NewOrderWrapper(newOrder, Config));

                            if (!processed.Success)
                                throw processed.Exception;
                        }
                        catch (Exception ex)
                        {
                            Order newOrder = BuildOrder(pos, pos.Orders.Count, qty);
                            RejectedExecutionReportWrapper rejWrapper = new RejectedExecutionReportWrapper(newOrder, ex.Message);
                            OnMessageRcv(rejWrapper);

                        }
                    }
                }
            }
        }

        //There is no lastqty and CumQty>0. we cancel the order and inform the special situation
        protected void EvalExecutionReportQuality(Position posForOrder,ExecutionReport report)
        {
            //Tenemos una trade y no hay LastQty --> problematico
            if (report.ExecType == ExecType.Trade && (!report.LastQty.HasValue || report.LastQty.Value <= 0))
            {
                report.LastQty = report.CumQty;
                posForOrder.CumQty = 0; //---> it will be later pdated

                //It's not ok that there is no LastQty --> WE CANCEL THE ORDER AND WE DO THE BEST WE CAN WITH ExecReports
                CancelOrderWrapper cxlWrapper = new CancelOrderWrapper(posForOrder.GetCurrentOrder(), Config);
                OrderProxy.ProcessMessage(cxlWrapper);

                DoLog(string.Format("Cancelling order {0} for symbol {1} because of lack of LastQty on traded exec report. CumQty={2}",
                                    report.Order.ClOrdId, report.Order.Symbol, report.CumQty), Constants.MessageType.Error);
            }
        }

        protected override void ProcessExecutionReport(Wrapper wrapper)
        {
            
            lock (tLock)
            {
                try
                {
                    ExecutionReport report = ExecutionReportConverter.GetExecutionReport(wrapper, Config);

                    if (ActiveOrdersDict.ContainsKey(report.Order.ClOrdId))
                    {
                        Position posForOrder = ActiveOrdersDict[report.Order.ClOrdId];
                        EvalExecutionReportQuality(posForOrder,report);

                        if (report.IsCancelationExecutionReport())
                        {
                            posForOrder.LeavesQty = 0;
                            posForOrder.SetPositionStatusFromExecutionStatus(report.OrdStatus);
                            
                            report.CumQty = posForOrder.CumQty;
                            report.LeavesQty = 0;
                            
                            GenericExecutionReportWrapper genWrapper = new GenericExecutionReportWrapper(report);
                            OnMessageRcv(genWrapper);
                            
                            ActiveOrdersDict.Remove(report.Order.ClOrdId);
                            PendingPartialOrderPositionsDict.Remove(posForOrder.PosId);
                           
                        }
                        else if (report.OrdStatus==OrdStatus.PartiallyFilled)
                        {
                            posForOrder.CumQty += report.LastQty.Value;
                            posForOrder.LeavesQty = posForOrder.Qty - posForOrder.CumQty;
                            posForOrder.SetPositionStatusFromExecutionStatus(report.OrdStatus);

                            report.CumQty = posForOrder.CumQty;
                            report.LeavesQty = posForOrder.Qty.Value - report.CumQty;
                            report.Order.OrderQty = posForOrder.Qty;
                            
                            OnMessageRcv(new GenericExecutionReportWrapper(report));
                        }
                        else if (report.OrdStatus == OrdStatus.Filled)
                        {
                            posForOrder.CumQty += report.LastQty.Value;

                            if (posForOrder.CumQty >= posForOrder.Qty)
                            {
                                posForOrder.LeavesQty = 0;
                                posForOrder.SetPositionStatusFromExecution(ExecType.Trade);

                                report.CumQty = posForOrder.CumQty;
                                report.LeavesQty = 0;
                                report.Order.OrderQty = posForOrder.Qty;

                                GenericExecutionReportWrapper genWrapper = new GenericExecutionReportWrapper(report);
                                OnMessageRcv(genWrapper);

                            }
                            else
                            {
                                posForOrder.LeavesQty = posForOrder.Qty - posForOrder.CumQty;
                                posForOrder.SetPositionStatusFromExecutionStatus(OrdStatus.PartiallyFilled);

                                //The new order will be implemented on new market data

                                report.CumQty = posForOrder.CumQty;
                                report.LeavesQty = posForOrder.LeavesQty.Value;
                                report.Order.OrderQty = posForOrder.Qty;
                                report.OrdStatus = OrdStatus.PartiallyFilled;

                                OnMessageRcv(new GenericExecutionReportWrapper(report));
                            }

                            ActiveOrdersDict.Remove(report.Order.ClOrdId);
                            PendingPartialOrderPositionsDict.Remove(posForOrder.PosId);
                        }
                    }
                    else
                    {
                        DoLog(string.Format("Received Execution Report for unknown order {0} for symbol {1}",report.Order.ClOrdId,report.Order.Security.Symbol),Constants.MessageType.Debug);
                    }

                }
                catch (Exception ex)
                {
                    DoLog(string.Format("CRITICAL error processing execution report @Soft Market Order Router :{0}", ex.Message), Constants.MessageType.Error);

                }
            }
        }

        protected CMState ProcessNewPosition(Wrapper wrapper)
        {
            Position newPos = PositionConverter.GetPosition(wrapper, Config);

            newPos.CumQty = 0;
            newPos.LeavesQty = 0;
            newPos.PosStatus = PositionStatus.PendingNew;

            lock (tLock)
            {
                PositionsDict.Add(newPos.PosId, newPos);
            }
            return CMState.BuildSuccess();
        }

        #endregion

        #region Public Methods

        public override CMState ProcessMessage(Wrapper wrapper)
        {
            try
            {
                if (wrapper.GetAction() == Actions.NEW_POSITION)
                {
                    string symbol = Convert.ToString(wrapper.GetField(PositionFields.Symbol));
                    DoLog(string.Format("Routing to market position for symbol {0} @SoftMarket Order Router", symbol), Constants.MessageType.Information);
                    return ProcessNewPosition(wrapper);
                }
                else if (wrapper.GetAction() == Actions.MARKET_DATA)
                {
                    ProcessMarketData(wrapper);
                    return CMState.BuildSuccess();
                }
                else
                {
                    return base.ProcessMessage(wrapper);
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("Error processing message @Soft Market Order Router for action {0}. Error: {1}", wrapper.GetAction().ToString(), ex.Message), Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                base.Initialize(pOnMessageRcv, pOnLogMsg, configFile);

                PositionsDict = new Dictionary<string, Position>();

                PendingPartialOrderPositionsDict = new Dictionary<string, Position>();

                ActiveOrdersDict = new Dictionary<string, Position>();

                tLock = new object();

                return true;
            }
            catch (Exception ex)
            {
                DoLog("Critic error initializing " + configFile + ":" + ex.Message, Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}

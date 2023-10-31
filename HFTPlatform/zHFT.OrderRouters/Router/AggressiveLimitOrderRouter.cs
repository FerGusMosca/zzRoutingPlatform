using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Common.Converters;
using zHFT.OrderRouters.Common.Wrappers;

namespace zHFT.OrderRouters.Router
{
    public class AggressiveLimitOrderRouter : DecimalOrderRouter
    {
        #region Overriden Methods

        protected override void ProcessMarketData(Wrapper wrapper)
        {

            lock (tLockCalculus)
            {
                string symbol = wrapper.GetField(MarketDataFields.Symbol).ToString();
                List<Position> positions= Positions.Values.Where(x => x.PositionRouting() && x.Security.Symbol == symbol).ToList();
                foreach (Position pos in positions)
                {
                    if (pos != null && !pos.PositionCleared && !pos.PositionCanceledOrRejected)
                    {
                        MarketData updMarketData = MarketDataConverter.GetMarketData(wrapper, Config);

                        if (pos.Side == Side.Buy)
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
                                        "Updating DOM price on ASK. Symbol: {0} - New Ask Price:{1} Old Ask Price:{2}",
                                        pos.Security.Symbol,
                                        pos.Security.MarketData.BestAskPrice.Value,
                                        updMarketData.BestAskPrice.Value), Constants.MessageType.Information);
                                    pos.NewDomFlag = true;
                                }
                            }
                        }
                        else if (pos.Side == Side.Sell)
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
                                        "Updating DOM price on BID. Symbol: {0} - New Bid Price:{1} Old Bid Price:{2}",
                                        pos.Security.Symbol,
                                        pos.Security.MarketData.BestBidPrice.Value,
                                        updMarketData.BestBidPrice.Value), Constants.MessageType.Information);
                                    pos.NewDomFlag = true;
                                }
                            }

                        }

                        pos.Security.MarketData = updMarketData;
                    }
                }
            }
        }

        protected override void EvalUpdPosOnNewMarketData(Position pos)
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
                        order.Price = pos.Security.MarketData.BestAskPrice;
                    else if (pos.Side == Side.Sell)
                        order.Price = pos.Security.MarketData.BestBidPrice; 
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
                        DoLog(string.Format("@Order Router: Updating order for symbol {0}: qty={1} price={2}", pos.Symbol, order.OrderQty, order.Price), Main.Common.Util.Constants.MessageType.Information);
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

        #endregion
    }
}

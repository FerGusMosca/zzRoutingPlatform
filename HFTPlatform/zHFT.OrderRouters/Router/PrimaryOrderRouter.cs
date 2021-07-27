using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.Common.Enums;
using zHFT.OrderRouters.Common.Wrappers;

namespace zHFT.OrderRouters.Router
{
    public class PrimaryOrderRouter : OrderRouter
    {
        #region Protected Overriden Methods

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
                        order.Price = pos.Security.MarketData.BestBidPrice;
                    else if (pos.Side == Side.Sell)
                        order.Price = pos.Security.MarketData.BestAskPrice;
                    else
                        throw new Exception("Invalid position side for Symbol " + pos.Security.Symbol);

                    //@Primary una vez que se especificó la cantidad, no se puede cambiar la misma
                    order.OrderQty = pos.LeavesQty;//Una vez que 

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

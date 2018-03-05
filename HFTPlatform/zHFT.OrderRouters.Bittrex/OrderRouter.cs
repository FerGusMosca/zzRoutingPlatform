using Bittrex;
using Bittrex.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.Common.Abstract;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Wrappers;
using zHFT.OrderRouters.Bittrex.BusinessEntities;
using zHFT.OrderRouters.Bittrex.Common.Wrappers;
using zHFT.OrderRouters.Bittrex.DataAccessLayer.Managers;
using zHFT.OrderRouters.Common;
using zHFT.OrderRouters.Cryptos;

namespace zHFT.OrderRouters.Bittrex
{
    public class OrderRouter : BaseOrderRouter
    {

        #region Private Static Const

        private string _INSUFFICIENT_FUNDS = "INSUFFICIENT_FUNDS";

        private string MIN_TRADE_REQUIREMENT_NOT_MET = "MIN_TRADE_REQUIREMENT_NOT_MET";

        #endregion

        #region Protected Attributes

        protected Common.Configuration.Configuration BittrexConfiguration { get; set; }

        private Dictionary<string, bool> ReverseCurrency { get; set; }

        protected AccountBittrexDataManager AccountBittrexDataManager { get; set; }

        #endregion

        #region Overriden Methods

        protected override BaseConfiguration GetConfig()
        {
            return BittrexConfiguration;
        }

        protected override string GetQuoteCurrency()
        {
            return BittrexConfiguration.QuoteCurrency;
        }

        protected override void DoLoadConfig(string configFile, List<string> noValueFields)
        {
            BittrexConfiguration = new Common.Configuration.Configuration().GetConfiguration<Common.Configuration.Configuration>(configFile, noValueFields);
        }

        #endregion

        #region Protected OrderRouterBase Methods

        protected ExchangeContext GetContext()
        {
            return new ExchangeContext()
            {
                ApiKey = BittrexConfiguration.ApiKey,
                QuoteCurrency = BittrexConfiguration.QuoteCurrency,
                Secret = BittrexConfiguration.Secret,
                Simulate = BittrexConfiguration.Simulate
            };
        }

        protected void DoEvalExecutionReport()
        {
            bool active=true;
            while(active)
            {
                Thread.Sleep(BittrexConfiguration.RefreshExecutionReportsInMilisec);
                List<ExecutionReportWrapper> wrappersToPublish = new List<ExecutionReportWrapper>();

                try
                {
                    lock (tLock)
                    {
                        List<string> uuidToRemove = new List<string>();

                        foreach (string uuid in ActiveOrders.Keys)
                        {

                            Order order = ActiveOrders[uuid];
                            GetOrderResponse ordResp = RunGetOrder(uuid);

                            if (ordResp != null)
                            {
                                ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, ordResp);
                                OrdStatus status = (OrdStatus)wrapper.GetField(ExecutionReportFields.OrdStatus);

                                if (Order.FinalStatus(status))
                                {
                                    uuidToRemove.Add(uuid);
                                    DoLog(string.Format("@{0}:Removing Order For Status:{1}", BittrexConfiguration.Name, status.ToString()), Main.Common.Util.Constants.MessageType.Debug);

                                }
                                else
                                {

                                }

                                wrappersToPublish.Add(wrapper);
                            }
                            else
                            {
                                ordResp = GetTheoreticalResponse(order, uuid);
                                ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, ordResp);
                                OrdStatus status = (OrdStatus)wrapper.GetField(ExecutionReportFields.OrdStatus);

                                wrappersToPublish.Add(wrapper);
                                uuidToRemove.Add(uuid);
                                DoLog(string.Format("@{0}:Removing Order Because no order could be found on market for uuid {1}", BittrexConfiguration.Name, uuid), Main.Common.Util.Constants.MessageType.Debug);
                            }
                        }
                        uuidToRemove.ForEach(x => ActiveOrders.Remove(x));
                    }

                    wrappersToPublish.ForEach(x => OnMessageRcv(x));
                    wrappersToPublish.Clear();
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Error processing execution reports!:{1}", BittrexConfiguration.Name, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                }
            }
        }

        protected void EvalRouteError(Order order, Exception ex)
        {
            GetOrderResponse ordResp = GetTheoreticalResponse(order, "");
            order.OrdStatus = OrdStatus.Rejected;
            ordResp.CancelInitiated = true;


            if (ex.Message.Contains(_INSUFFICIENT_FUNDS))
            {
                order.RejReason = _INSUFFICIENT_FUNDS;
            }
            else if (ex.Message.Contains(MIN_TRADE_REQUIREMENT_NOT_MET))
            {
                order.RejReason = MIN_TRADE_REQUIREMENT_NOT_MET;
            }
            else
                order.RejReason = ex.Message;

            ExecutionReportWrapper wrapper = new ExecutionReportWrapper(order, ordResp);
            OnMessageRcv(wrapper);
        }

        protected override CMState RouteNewOrder(Wrapper wrapper)
        {
            try
            {
                Order order = GetOrder(wrapper);
                try
                {
                    lock (tLock)
                    {
                        RunNewOrder(order, false);
                    }
                }
                catch (Exception ex)
                {
                    EvalRouteError(order, ex);
                }
               
                return CMState.BuildSuccess();
            }
            catch (Exception ex)
            {
                return CMState.BuildFail(ex);
            }
            
        }

        protected GetOrderResponse RunGetOrder(string uuid)
        {
            Exchange exchange = new Exchange();
            ExchangeContext ctx = GetContext();
            exchange.Initialise(ctx);
            return exchange.GetOrder(uuid);
        
        }

        private GetOrderResponse GetTheoreticalResponse(Order order, string uuid)
        {
            decimal quantityRemaining = Convert.ToDecimal(order.OrderQty.Value);
            //evaluamos la orden como ejecutada o cancelada
            if (CanceledOrders.Contains(uuid))
            {
                CanceledOrders.Remove(uuid);
                quantityRemaining = 0;
            }

            return new GetOrderResponse()
            {
                OrderUuid = uuid,
                QuantityRemaining = quantityRemaining,//0=cancelada, xx=ejecutada
                Quantity = Convert.ToDecimal(order.OrderQty.Value),
                Price = Convert.ToDecimal(order.Price),
                Limit = Convert.ToDecimal(order.Price)
            };
        }

        private void DoReverse(ref string symbol, ref double ordQty, ref Side side, double price, Exchange exchange, ExchangeContext ctx)
        {
            string tempSymbol = symbol;
            symbol = ctx.QuoteCurrency;
            ctx.QuoteCurrency = tempSymbol;
            exchange.Initialise(ctx);

            if (side == Side.Buy)
                ordQty = ordQty * price;
            else if (side == Side.Sell)
                ordQty = ordQty / price; 

            side = side == Side.Buy ? Side.Sell : Side.Buy;
            

        }

        protected void EvalReverse(ref string symbol, ref double ordQty, ref Side side, double price, 
                                Exchange exchange, ExchangeContext ctx)
        {
            if (!ReverseCurrency.Keys.Contains(symbol))
            {
                try
                {
                    JObject jMarketData = exchange.GetTicker(symbol);
                    ReverseCurrency.Add(symbol, false);

                }
                catch (Exception ex)
                {
                    DoReverse(ref symbol, ref ordQty, ref side, price, exchange, ctx);
                    JObject jMarketData = exchange.GetTicker(symbol);
                    ReverseCurrency.Add(ctx.QuoteCurrency, true);
                }
            }
            else
            {
                if (ReverseCurrency[symbol])
                {
                    DoReverse(ref symbol, ref ordQty, ref side, price, exchange, ctx);
                }
            }
        }

        private void RunNewOrder(Order order, bool update)
        {
            Exchange exchange = new Exchange();
            ExchangeContext ctx = GetContext();
            exchange.Initialise(ctx);

            string symbol = order.Symbol;
            Side side = order.Side;
            double ordQty = order.OrderQty.Value;
            double price = order.Price.Value;

            EvalReverse(ref symbol, ref ordQty, ref side, price, exchange, ctx);

            if (side == Side.Buy)
            {
                
                OrderResponse resp = exchange.PlaceBuyOrder(symbol,
                                                            Convert.ToDecimal(ordQty),
                                                            Convert.ToDecimal(price));

                if (resp != null)
                {
                    order.OrderId = resp.uuid;
                    ActiveOrders.Add(resp.uuid, order);
                    OrderIdMappers.Add(order.ClOrdId, resp.uuid);
                }
                else throw new Exception(string.Format("Unknown error routing order for currency {0}", order.Symbol));
            }
            else if (side == Side.Sell)
            {
                OrderResponse resp = exchange.PlaceSellOrder(symbol,
                                                            Convert.ToDecimal(ordQty),
                                                            Convert.ToDecimal(price));

                if (resp != null)
                {
                    order.OrderId = resp.uuid;
                    ActiveOrders.Add(resp.uuid, order);
                    OrderIdMappers.Add(order.ClOrdId, resp.uuid);
                }
                else throw new Exception(string.Format("Unknown error routing order for currency {0}", order.Symbol));
            }
            else
                throw new Exception(string.Format("Invalid value for side:{0}", order.Side.ToString()));
        }

        protected override bool RunCancelOrder(Order order,bool update)
        {
            Exchange exchange = new Exchange();
            ExchangeContext ctx = GetContext();
            exchange.Initialise(ctx);
            DoLog(string.Format("@{0}:Cancelling Order Id {1} for symbol {2}", BittrexConfiguration.Name, order.OrderId, order.Symbol), Main.Common.Util.Constants.MessageType.Information);
            
            exchange.CancelOrder(order.OrderId);

            if (!update)
                CanceledOrders.Add(order.OrderId);
            else
            {
                ActiveOrders.Remove(order.OrderId);//Ya no actuallizamos mas datos de la vieja orden cancelada
            }

            return true;
        }

        protected override CMState UpdateOrder(Wrapper wrapper)
        {
          
                string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();

                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();
                try
                {

                    if (wrapper.GetField(OrderFields.OrigClOrdID) == null)
                        throw new Exception("Could not find OrigClOrdID for order updated");

                    if (wrapper.GetField(OrderFields.ClOrdID) == null)
                        throw new Exception("Could not find ClOrdId for new order");

                    lock (tLock)
                    {

                        if (OrderIdMappers.ContainsKey(origClOrderId))
                        {
                            string origUuid = OrderIdMappers[origClOrderId];
                            Order order = ActiveOrders[origUuid];

                            if (order != null)
                            {
                                //Cancelamos
                                RunCancelOrder(order,true);

                                Thread.Sleep(100);

                                //Damos el alta
                                double? newPrice = (double?)wrapper.GetField(OrderFields.Price);
                                order.Price = newPrice;
                                order.ClOrdId = clOrderId;
                                try
                                {
                                    RunNewOrder(order, true);
                                }
                                catch (Exception ex)
                                {
                                    EvalRouteError(order, ex);
                                }
                            }

                        }
                        else
                            DoLog(string.Format("@{0}:Could not find order for origClOrderId  {1}!", BittrexConfiguration.Name, origClOrderId), Main.Common.Util.Constants.MessageType.Error);

                    }

                    return CMState.BuildSuccess();
                }
                catch (Exception ex)
                {
                    DoLog(string.Format("@{0}:Error updating order {1}!:{2}", BittrexConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                    return CMState.BuildFail(ex);
                }
        }

        protected override CMState CancelOrder(Wrapper wrapper)
        {
            string origClOrderId = wrapper.GetField(OrderFields.OrigClOrdID).ToString();
            try
            {
                //New order id
                string clOrderId = wrapper.GetField(OrderFields.ClOrdID).ToString();

                lock (tLock)
                {

                    if (OrderIdMappers.ContainsKey(origClOrderId))
                    {
                        string uuid = OrderIdMappers[origClOrderId];
                        Order order = ActiveOrders[uuid];

                        if (order != null)
                        {
                            RunCancelOrder(order, false);
                        }
                        OrderIdMappers.Remove(origClOrderId);
                    }
                   
                    else
                    {
                        throw new Exception(string.Format("Could not cancel order for id {0}", origClOrderId));
                        //TO DO: La orden fue modificada
                        //buscar con el nuevo clOrderId
                    }
                }
                return CMState.BuildSuccess();
                   
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Error cancelig order {1}!:{2}", BittrexConfiguration.Name, origClOrderId, ex.Message), Main.Common.Util.Constants.MessageType.Error);
                return CMState.BuildFail(ex);
            }
        }

        #endregion

        #region Public Methods

        public override bool Initialize(OnMessageReceived pOnMessageRcv, OnLogMessage pOnLogMsg, string configFile)
        {
            try
            {
                this.ModuleConfigFile = configFile;
                this.OnMessageRcv += pOnMessageRcv;
                this.OnLogMsg += pOnLogMsg;

                if (LoadConfig(configFile))
                {
                    tLock = new object();

                    ActiveOrders = new Dictionary<string, Order>();
                    CanceledOrders = new List<string>();
                    ReverseCurrency = new Dictionary<string, bool>();

                    AccountBittrexDataManager = new AccountBittrexDataManager(BittrexConfiguration.ConfigConnectionString);

                    OrderIdMappers = new Dictionary<string, string>();

                    ExecutionReportThread = new Thread(DoEvalExecutionReport);
                    ExecutionReportThread.Start();

                    //Todo inicializar mundo Bittrex
                    AccountBittrexData bittrexData = AccountBittrexDataManager.GetByAccountNumber(BittrexConfiguration.AccountNumber);

                    if (bittrexData == null)
                        throw new Exception(string.Format("No se encontró ninguna configuración de autenticación contra Bittrex de la cuenta {0}", BittrexConfiguration.AccountNumber));

                    BittrexConfiguration.ApiKey = bittrexData.APIKey;
                    BittrexConfiguration.Secret = bittrexData.Secret;

                    return true;
                }
                else
                {
                    DoLog(string.Format("@{0}:Error initializing config file " + configFile,BittrexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                DoLog(string.Format("@{0}:Critic error initializing " + configFile + ":" + ex.Message,BittrexConfiguration.Name), Main.Common.Util.Constants.MessageType.Error);
                return false;
            }
        }

        #endregion
    }
}
